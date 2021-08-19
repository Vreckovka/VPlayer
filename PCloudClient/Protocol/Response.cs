using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Hash = System.Collections.Generic.Dictionary<string, object>;

namespace PCloud
{
	/// <summary>Parsed response from pCloud's server.</summary>
	class Response
	{
		/// <summary>Payload object of the response.</summary>
		public struct Data
		{
			public readonly long payloadLength;
			public Data( long len )
			{
				payloadLength = len;
			}
		}

		/// <summary>First JSON dictionary in the response.</summary>
		public readonly IReadOnlyDictionary<string, object> dict;

		/// <summary>Payload[s] in the response, if any. Otherwise null.</summary>
		public readonly Data[] payload = null;

		/// <summary>Other values in the response, if any. Otherwise null.</summary>
		public readonly object[] otherValues = null;

		/// <summary>Read response from the network stream.</summary>
		/// <remarks>If the response contains payload portion, i.e. files, they are <b>not</b> read from the stream.
		/// It's caller's responsibility to iterate through <see cref="payload" /> values and read all the data before reusing the connection for other requests.</remarks>
		public static async Task<Response> receive( Stream connection )
		{
			// Receive into a buffer
			using( var buffer = await ResponseBuffer.receive( connection ) )
			{
				// Parse binary JSON from that buffer
				Hash dict = null;
				List<object> otherValues = null;
				var parser = new ResponseParser( buffer );

				foreach( object obj in parser.parse() )
				{
					switch( obj )
					{
						case Hash h:
							if( null == dict )
							{
								dict = h;
								break;
							}
							goto default;
						default:
							if( null == otherValues )
								otherValues = new List<object>();
							otherValues.Add( obj );
							break;
					}
				}
				// Return a new response object
				return new Response( dict, parser.payloads, otherValues );
			}
		}

		Response( Hash dict, List<Data> payload, List<object> otherValues )
		{
			this.dict = dict;
			if( null != payload )
				this.payload = payload.ToArray();
			if( null != otherValues )
				this.otherValues = otherValues.ToArray();
		}

		/// <summary>if this response is empty, or contains failed status code, create an exception telling that. Return null if the response says it was completed successfully.</summary>
		public Exception exception()
		{
			if( null == dict )
				return new ApplicationException( "The response has no dictionary" );

			object objResult;
			if( !dict.TryGetValue( "result", out objResult ) )
				return new ApplicationException( "The response has no \"result\" property" );

			int result = Convert.ToInt32( objResult );
			if( 0 == result )
				return null; // Normally result: 0 means success while a non-zero result means an error

			string localCode = ErrorCodes.lookup( result );
			string remoteCode = dict.lookup( "error" ) as string;
			if( null == localCode )
			{
				if( null == remoteCode )
					return new ApplicationException( $"Operation failed, code { result }" );
				return new ApplicationException( $"Operation failed, code { result }: { remoteCode }" );
			}
			if( localCode == remoteCode )
				return new ApplicationException( $"Operation failed, code { result }: { localCode }" );
			return new ApplicationException( $"Operation failed, code { result }: { localCode } / { remoteCode }" );
		}

		/// <summary>Get property from the response</summary>
		public object this[ string key ]
		{
			get
			{
				object result = dict.lookup( key );
				if( null == result )
					throw new ApplicationException( $"The response lacks the required property \"{ key }\"" );
				return result;
			}
		}
	}
}