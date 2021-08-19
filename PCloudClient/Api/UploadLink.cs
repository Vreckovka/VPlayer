using System;
using System.IO;
using System.Threading.Tasks;

namespace PCloud
{
	/// <summary>Public link RPCs</summary>
	public static class UploadLink
	{
		/// <summary>Anonymously upload file to an upload link.</summary>
		/// <remarks>
		/// <para>Works without authentication, you need upload link code to use this API.</para>
		/// <para>The API documentation is silent about that, but "names" request parameter appears to be mandatory for this RPC.</para>
		/// </remarks>
		public static Task uploadToLink( this Connection conn, string fileName, Stream payload, string uploadLinkCode, string from, bool nopartial = true )
		{
			if( string.IsNullOrWhiteSpace( fileName ) )
				throw new ArgumentException( "File name can't be empty", "fileName" );
			if( string.IsNullOrWhiteSpace( from ) )
				throw new ArgumentException( "Sender name can't be empty", "from" );

			var req = new RequestBuilder( "uploadtolink", payload.Length );
			req.add( "names", from );
			req.add( "filename", fileName );
			req.add( "code", uploadLinkCode );
			req.add( "nopartial", nopartial );

			payload.rewind();
			return conn.upload( req, payload );
		}
	}
}