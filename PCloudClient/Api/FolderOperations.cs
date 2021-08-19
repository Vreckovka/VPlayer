using System;
using System.Threading.Tasks;

namespace PCloud
{
	/// <summary>Folder operations RPCs</summary>
	public static class FolderOperations
	{
		/// <summary>Create a folder</summary>
		/// <param name="name">Name of the new folder</param>
		/// <param name="parent">Parent folder, or null to create a top-level one</param>
		/// <param name="getExisting">true to get an existing one if it exists, false to fail</param>
		public static async Task<Metadata.FolderInfo> createFolder( this Connection conn, string name, Metadata.FolderInfo parent = null, bool getExisting = true )
		{
			RequestBuilder req = conn.newRequest( getExisting ? "createfolderifnotexists" : "createfolder" );
			req.add( "folderid", parent?.id ?? 0L );
			req.add( "name", name );
			req.unixTimestamps();

			var response = await conn.send( req );
			return new Metadata.FolderInfo( response.metadata() );
		}

		/// <summary>Delete a folder</summary>
		public static Task deleteFolder( this Connection conn, Metadata.FolderInfo folder, bool recursively = false )
		{
			if( null == folder || 0 == folder.id )
				throw new ArgumentException( "The root folder can't be deleted" );
			RequestBuilder req = conn.newRequest( recursively ? "deletefolderrecursive" : "deletefolder" );
			req.add( "folderid", folder.id );
			req.unixTimestamps();

			return conn.send( req );
		}

		/// <summary>Rename and/or move a folder</summary>
		public static async Task<Metadata.FolderInfo> renameFolder( this Connection conn, Metadata.FolderInfo folder, string newName, Metadata.FolderInfo newParent )
		{
			if( null == folder || 0 == folder.id )
				throw new ArgumentException( "The root folder can't be renamed" );

			if( string.IsNullOrEmpty( newName ) && null == newParent )
				throw new ArgumentException( "Neither new name nor new parent is specified." );

			RequestBuilder req = conn.newRequest( "renamefolder" );
			req.add( "folderid", folder.id );
			if( !string.IsNullOrEmpty( newName ) )
				req.add( "toname", newName );
			if( null != newParent )
				req.add( "tofolderid", newParent.id );
			req.unixTimestamps();

			var response = await conn.send( req );
			return new Metadata.FolderInfo( response.metadata() );
		}

		/// <summary>Rename a folder</summary>
		public static Task<Metadata.FolderInfo> renameFolder( this Connection conn, Metadata.FolderInfo folder, string newName )
		{
			return conn.renameFolder( folder, newName, null );
		}

		/// <summary>Move a folder</summary>
		public static Task<Metadata.FolderInfo> moveFolder( this Connection conn, Metadata.FolderInfo folder, Metadata.FolderInfo newParent )
		{
			return conn.renameFolder( folder, null, newParent );
		}
	}
}