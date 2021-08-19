using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PCloud
{
	/// <summary>Connect to the server, optionally setting up SSL traffic encryption</summary>
	static class Endpoint
	{
		public const string host = "api.pcloud.com";
		const int port = 8398;
		const int portSsl = 8399;

		static async Task<Stream> connectTcp( string hostDnsName, int tcpPortNumber )
		{
			TcpClient client = new TcpClient();
			try
			{
				await client.ConnectAsync( hostDnsName, tcpPortNumber );
				return client.GetStream();
			}
			catch( Exception )
			{
				client.Dispose();
				throw;
			}
		}

		static async Task<Stream> connectSsl( string hostDnsName )
		{
			Stream tcp = await connectTcp( hostDnsName, portSsl );
			SslStream sslStream = new SslStream( tcp, false );
			try
			{
				await sslStream.AuthenticateAsClientAsync( host );
				return sslStream;
			}
			catch( Exception )
			{
				sslStream.Dispose();
				throw;
			}
		}

		/// <summary>Establish connection to a pCloud server.</summary>
		public static Task<Stream> connect( bool encryptTraffic, string hostDnsName = host )
		{
			return encryptTraffic ? connectSsl( hostDnsName ) : connectTcp( hostDnsName, port );
		}
	}
}