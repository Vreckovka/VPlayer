using System;
using System.Threading.Tasks;
using System.Reflection;

namespace PCloud
{
	/// <summary>RPCs related to authentication</summary>
	public static class Authentication
	{
		static Authentication()
		{
			Assembly ass = Assembly.GetEntryAssembly();
			var apa = ass.GetCustomAttribute<AssemblyProductAttribute>();
			if( null != apa )
				deviceInfoString = $"{ apa.Product }, { ass.GetName().Version }";
			else
				deviceInfoString = ass.FullName;
		}

		/// <summary>Apparently, their web interface shows that data for live sessions. Good idea to set into something more descriptive before using the library.</summary>
		public static string deviceInfoString;

		static async Task<string> getDigest( this Connection conn )
		{
			var request = new RequestBuilder( "getdigest" );
			var response = await conn.send( request );
			return (string)response[ "digest" ];
		}

		/// <summary>Login with e-mail and password</summary>
		public static async Task login( this Connection conn, string email, string password )
		{
			if( conn.isAuthenticated )
				await conn.logout();

			string digest = await conn.getDigest();
			string passwordDigest = Utils.sha1( password + Utils.sha1( email.ToLowerInvariant() ) + digest );
			var req = new RequestBuilder( "userinfo" );
			req.add( "getauth", true );
			req.add( "username", email );
			req.add( "digest", digest );
			req.add( "passworddigest", passwordDigest );
			// Set device global parameter
			req.add( "device", deviceInfoString );
			var response = await conn.send( req );
			conn.authToken = (string)response.dict[ "auth" ];
		}

		/// <summary>Logout</summary>
		public static async Task logout( this Connection conn )
		{
			var req = conn.newRequest( "logout" );
			var response = await conn.send( req );
			if( !(bool)response[ "auth_deleted" ] )
				throw new ApplicationException( "Unable to logout" );
			conn.authToken = null;
		}

		/// <summary>Change current user's password; requires SSL encrypted server connection.</summary>
		public static Task changePassword( this Connection conn, string oldPassword, string newPassword )
		{
			if( !conn.isEncrypted )
				throw new NotSupportedException( "For security reasons, you must use encrypted connection to change a password." );

			var req = conn.newRequest( "changepassword" );
			req.addSecret( "oldpassword", oldPassword );
			req.addSecret( "newpassword", newPassword );
			return conn.send( req );
		}
	}
}