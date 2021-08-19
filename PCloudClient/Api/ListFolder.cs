using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PCloud
{
	/// <summary>Folder listing RPCs</summary>
	public static class ListFolder
	{
		/// <summary>List directories, and optionally files. This is relatively low-level method, direct wrapper over the RPC.</summary>
		public static async Task<Metadata.FolderInfo> listFolder( this Connection conn, long id = 0, bool recursive = false, bool noFiles = false )
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
			return (Metadata.FolderInfo)Metadata.FileBase.create( meta );
		}

		/// <summary>Get folder by name, the name is case sensitive. Returns null if not exist.</summary>
		public static async Task<Metadata.FolderInfo> getFolder( this Connection conn, string name, long idParent = 0 )
		{
			var folder = await conn.listFolder( idParent, false, true );
			return folder?.children?.OfType<Metadata.FolderInfo>().FirstOrDefault( fi => fi.name == name );
		}

		/// <summary>List all files in the folder, the folder is specified with ID</summary>
		public static async Task<Metadata.FileInfo[]> getFiles( this Connection conn, long idFolder )
		{
			// Console.WriteLine( "getFiles( {0:x} )", idFolder );
			var folder = await conn.listFolder( idFolder, false, false );
			var result = folder.children.OfType<Metadata.FileInfo>().ToArray();
			// Console.WriteLine( "getFiles( {0:x} ) - got {1} files", idFolder, result.Length );
			return result;
		}

		/// <summary>List all files in the folder</summary>
		public static Task<Metadata.FileInfo[]> getFiles( this Connection conn, Metadata.FolderInfo fi )
		{
			return conn.getFiles( fi.id );
		}
	}
}