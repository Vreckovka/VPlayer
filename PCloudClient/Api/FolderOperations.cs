using System;
using System.Threading.Tasks;
using PCloudClient.Domain;

namespace PCloud
{
	/// <summary>Folder operations RPCs</summary>
	public static class FolderOperations
	{
		/// <summary>Create a folder</summary>
		/// <param name="name">Name of the new folder</param>
		/// <param name="parent">Parent folder, or null to create a top-level one</param>
		/// <param name="getExisting">true to get an existing one if it exists, false to fail</param>
		public static async Task<FolderInfo> createFolder( this Connection conn, string name, FolderInfo parent = null, bool getExisting = true )
		{
			RequestBuilder req = conn.newRequest( getExisting ? "createfolderifnotexists" : "createfolder" );
			req.add( "folderid", parent?.id ?? 0L );
			req.add( "name", name );
			req.unixTimestamps();

			var response = await conn.send( req );
			return new FolderInfo( response.metadata() );
		}

		/// <summary>Delete a folder</summary>
		public static Task deleteFolder( this Connection conn, FolderInfo folder, bool recursively = false )
		{
			if( null == folder || 0 == folder.id )
				throw new ArgumentException( "The root folder can't be deleted" );
			RequestBuilder req = conn.newRequest( recursively ? "deletefolderrecursive" : "deletefolder" );
			req.add( "folderid", folder.id );
			req.unixTimestamps();

			return conn.send( req );
		}

		/// <summary>Rename and/or move a folder</summary>
		public static async Task<FolderInfo> renameFolder( this Connection conn, FolderInfo folder, string newName, FolderInfo newParent )
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
			return new FolderInfo( response.metadata() );
		}

		/// <summary>Rename a folder</summary>
		public static Task<FolderInfo> renameFolder( this Connection conn, FolderInfo folder, string newName )
		{
			return conn.renameFolder( folder, newName, null );
		}

		/// <summary>Move a folder</summary>
		public static Task<FolderInfo> moveFolder( this Connection conn, FolderInfo folder, FolderInfo newParent )
		{
			return conn.renameFolder( folder, null, newParent );
		}
	}
}