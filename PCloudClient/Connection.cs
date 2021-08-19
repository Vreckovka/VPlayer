using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PCloud
{
	/// <summary>Thrown when trying to use connection that failed at a wrong moment, and can't be reused for more requests.</summary>
	public class DesyncException: ApplicationException
	{
		internal DesyncException( Exception inner ) :
			base( "The connection is out of sync due to an IO error. It's not good anymore, you need to reconnect.", inner )
		{ }
	}

	/// <summary>Wraps TCP or SSL stream.</summary>
	/// <remarks>
	/// <para>User-facing functionality is implemented in separate static files in Api subfolder of the project. This class only implements low-level stuff, the most complex part is multiplexing.</para>
	/// <para>Hopefully, all public methods are thread safe and reentrant.</para>
	/// </remarks>
	public class Connection: IDisposable
	{
		readonly Stream stream;

		/// <summary>Cache the delegate for the most common response handling. It's used by all requests except download ones, no need to produce unneeded garbage.</summary>
		readonly Func<Task<Response>> fnReceiveSimple;

		/// <summary>Host name of the server</summary>
		public readonly string serverDnsName;

		/// <summary>True if this connection is encrypted.</summary>
		public readonly bool isEncrypted;

		Connection( Stream stream, string serverDnsName, bool isEncrypted )
		{
			this.stream = stream;
			this.serverDnsName = serverDnsName;
			this.isEncrypted = isEncrypted;
			fnReceiveSimple = receiveSimple;
		}

		/// <summary>Close connection and underlying network stream</summary>
		public void Dispose()
		{
			stream?.Dispose();
		}

		/// <summary>Open a new connection to pCloud</summary>
		/// <param name="encryptTraffic">True to use SSL for the connection.</param>
		/// <param name="authenticationToken">If you have an auth. token, you can make new connection already authenticated.</param>
		public static async Task<Connection> open( bool encryptTraffic, string host = Endpoint.host, string authenticationToken = null )
		{
			Stream stream = await Endpoint.connect( encryptTraffic, host );
			return new Connection( stream, Endpoint.host, encryptTraffic ) { authToken = authenticationToken };
		}

		/// <summary>Open a connection to the geographically closest pCloud server.</summary>
		/// <remarks>
		/// <para>Doesn't work in practice. I'm getting binapiams2.pcloud.com for the nearest binary API server, however TCP ports 8398 and 8399 are both closed.</para>
		/// <para>The observed behavior it documented:
		/// <i>If request to API server different from api.pcloud.com fails (network error) the client should fall back to using api.pcloud.com.</i></para>
		/// </remarks>
		public static async Task<Connection> openNearest( bool encryptTraffic )
		{
			Connection defaultConnection = await open( encryptTraffic );
			string[] nearestServers = null;
			try
			{
				nearestServers = await defaultConnection.getNearestServer();
			}
			catch( Exception )
			{
				return defaultConnection;
			}

			foreach( string host in nearestServers.enumerate() )
			{
				try
				{
					Stream stream = await Endpoint.connect( encryptTraffic, host );
					// Connected to a nearest server. Close the default connection, and return the new one instead.
					defaultConnection.Dispose();
					return new Connection( stream, host, encryptTraffic );
				}
				catch( Exception ) { }
			}
			return defaultConnection;
		}

		// Error handling
		volatile DesyncException desyncedException = null;

		/// <summary>True if this connections has failed and can't be reused.</summary>
		public bool isDesynced => null != desyncedException;

		/// <summary>Mark this connection as invalid due to lost synchronization with the server. Desynced connections can't be reused, they need to be reopened.</summary>
		void setDesynced( Exception ex )
		{
			Interlocked.CompareExchange( ref desyncedException, new DesyncException( ex ), null );
		}

		/// <summary>Only 1 thread can write socket at the same time. This semaphore enforces that limit.</summary>
		readonly SemaphoreSlim write = new SemaphoreSlim( 1 );
		volatile int pendingWriteLocks = 0;

		async Task aquireWriteLock()
		{
			Interlocked.Increment( ref pendingWriteLocks );
			await write.WaitAsync();
			Interlocked.Decrement( ref pendingWriteLocks );
			if( null != desyncedException )
			{
				write.Release();
				throw desyncedException;
			}
		}

		void releaseWriteLock()
		{
			write.Release();
		}

		/// <summary>True if some other thread or task is waiting for the lock to write another request. This is a performance optimization, in this case we don't flush the stream, for deeper queue.</summary>
		bool hasNextRequest => pendingWriteLocks > 0;

		readonly TaskQueue receiveQueue = new TaskQueue();

		async Task<Response> receiveSimple()
		{
			Response response = null;
			try
			{
				response = await Response.receive( stream );
			}
			catch( Exception ex )
			{
				setDesynced( ex );
				throw;
			}

			// If the response was received completely but contains failed status, the error doesn't cause desync, the same connection can be reused for other requests.
			Exception failed = response.exception();
			if( null == failed )
				return response;

			if( response.payload.notEmpty() )
			{
				// If the failed response came with payload, receive the payload to reuse the connection for other requests.
				long length = response.payload.Select( p => p.payloadLength ).Sum();
				try
				{
					await stream.fastForward( length );
				}
				catch( Exception ex )
				{
					setDesynced( ex );
					throw;
				}
			}
			throw failed;
		}

		async Task<Task<Response>> sendImpl( Func<Task<Response>> fnReceive )
		{
			bool flushedStream = false;
			// If there's no next requests waiting, flush the socket. Otherwise the network stack will only send the new request after a timeout.
			if( !hasNextRequest )
			{
				await stream.FlushAsync();
				flushedStream = true;
			}
			// Unwrap exactly one level of Task<> to limit count of in-flight requests.
			// Also, if the post() method gonna sleep on the throttle semaphore, flush the socket, unless already flushed above.
			bool wasThrottled;
			var tt = receiveQueue.post( fnReceive, out wasThrottled );
			if( wasThrottled && !flushedStream )
				await stream.FlushAsync();

			return await tt;
		}

		/// <summary>Simple request without payload in either direction</summary>
		internal async Task<Response> send( RequestBuilder requestBuilder )
		{
			Task<Response> tResult;
			await aquireWriteLock();
			try
			{
				await requestBuilder.close().CopyToAsync( stream );
				tResult = await sendImpl( fnReceiveSimple );
			}
			catch( Exception ex )
			{
				setDesynced( ex );
				throw;
			}
			finally
			{
				releaseWriteLock();
			}
			return await tResult;
		}

		/// <summary>Upload request which copies the supplied stream, either completely, or specified count of bytes</summary>
		internal async Task<Response> upload( RequestBuilder requestBuilder, Stream source, long length = -1 )
		{
			Task<Response> tResult;
			await aquireWriteLock();
			try
			{
				// Send the request
				await requestBuilder.close().CopyToAsync( stream );
				// Send the payload
				if( length < 0 )
					await source.CopyToAsync( stream );
				else
					await Utils.copyData( source, stream, length );
				tResult = await sendImpl( fnReceiveSimple );
			}
			catch( Exception ex )
			{
				setDesynced( ex );
				throw;
			}
			finally
			{
				releaseWriteLock();
			}
			return await tResult;
		}

		/// <summary>Download requests which writes specified count of payload bytes from socket to the provided stream</summary>
		internal async Task<Response> download( RequestBuilder requestBuilder, Stream dest, long length )
		{
			Func<Task<Response>> fnReceiveResult = async () =>
			{
				var resp = await receiveSimple();
				if( resp.payload.isEmpty() )
					throw new ApplicationException( $"Connection.download: expected { length } bytes, got no payload" );
				long payloadBytes = resp.payload.Sum( d => d.payloadLength );
				if( payloadBytes != length )
					throw new ApplicationException( $"Connection.download: expected { length } bytes, got { payloadBytes } in the response" );
				await stream.copyData( dest, length );
				return resp;
			};

			Task<Response> tResult;
			await aquireWriteLock();
			try
			{
				// Send the request
				await requestBuilder.close().CopyToAsync( stream );
				tResult = await sendImpl( fnReceiveResult );
			}
			catch( Exception ex )
			{
				setDesynced( ex );
				throw;
			}
			finally
			{
				releaseWriteLock();
			}
			return await tResult;
		}

		/// <summary>If this connection is authenticated, returns the token. You can use the token to open another connection which is already authenticated, without calling login.</summary>
		public string authToken { get; internal set; } = null;

		/// <summary>True if this connection is authenticated</summary>
		public bool isAuthenticated => !string.IsNullOrEmpty( authToken );

		/// <summary>Create a request builder; if authenticated, set `auth` property.</summary>
		internal RequestBuilder newRequest( string method, long? payloadLength = null )
		{
			var req = new RequestBuilder( method, payloadLength );
			string at = authToken;
			if( !string.IsNullOrEmpty( at ) )
				req.add( "auth", at );
			return req;
		}

 
	}
}