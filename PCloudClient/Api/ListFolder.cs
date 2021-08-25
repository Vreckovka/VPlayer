using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PCloudClient.Domain;

namespace PCloud
{
	/// <summary>Folder listing RPCs</summary>
	public static class ListFolder
	{
		/// <summary>List directories, and optionally files. This is relatively low-level method, direct wrapper over the RPC.</summary>
		public static async Task<FolderInfo> listFolder( this Connection conn, long id = 0, bool recursive = false, bool noFiles = false )
		{
			RequestBuilder req = conn.newRequest( "listfolder" );
			req.add( "folderid", id );
			req.add( "recursive", recursive );
			req.add( "nofiles", noFiles );
			req.unixTimestamps();
			var response = await conn.send( req );

			var meta = response.dict[ "metadata" ] as IReadOnlyDictionary<string, object>;
			if( null == meta )
				return null;
			return (FolderInfo)FileBase.create( meta );
		}

		/// <summary>Get folder by name, the name is case sensitive. Returns null if not exist.</summary>
		public static async Task<FolderInfo[]> getFolders( this Connection conn, long idFolder)
		{
      var folder = await conn.listFolder(idFolder, false, false);

      var result = folder.children.OfType<FolderInfo>().ToArray();
      // Console.WriteLine( "getFiles( {0:x} ) - got {1} files", idFolder, result.Length );
      return result;
		}

		/// <summary>List all files in the folder, the folder is specified with ID</summary>
		public static async Task<FileInfo[]> getFiles( this Connection conn, long idFolder )
		{
			// Console.WriteLine( "getFiles( {0:x} )", idFolder );
			var folder = await conn.listFolder( idFolder, false, false );
			var result = folder.children.OfType<FileInfo>().ToArray();
			// Console.WriteLine( "getFiles( {0:x} ) - got {1} files", idFolder, result.Length );
			return result;
		}

		/// <summary>List all files in the folder</summary>
		public static Task<FileInfo[]> getFiles( this Connection conn, FolderInfo fi )
		{
			return conn.getFiles( fi.id );
		}
	}
}