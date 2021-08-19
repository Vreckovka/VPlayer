using System;
using System.Collections.Generic;
using System.Linq;

namespace PCloud.Metadata
{
	/// <summary>Abstract base class for both folders and files. All data is read only.</summary>
	public abstract class FileBase
	{
		/// <summary>folderid of the folder the object resides in</summary>
		public readonly long parentFolderId;
		/// <summary>folderid or fileid</summary>
		public readonly long id;
		/// <summary>the name of file or folder</summary>
		public readonly string name;
		/// <summary>creation date of the object</summary>
		public readonly DateTime created;
		/// <summary>modification date of the object</summary>
		public readonly DateTime modified;
		/// <summary>icon to display</summary>
		public readonly eIcon icon;
		/// <summary>category of the file</summary>
		public readonly eCategory category;

		/// <summary>Set fields by converting values from that untyped binary JSON.</summary>
		protected internal FileBase( bool isFolder, IReadOnlyDictionary<string, object> dict )
		{
			parentFolderId = dict.getLong( "parentfolderid", 0 );
			id = dict.getLong( isFolder ? "folderid" : "fileid" );
			name = (string)dict[ "name" ];
			created = dict.getTimestamp( "created" );
			modified = dict.getTimestamp( "modified" );

			string iconString = dict.lookup( "icon" ) as string;
			if( null == iconString || !Enum.TryParse( iconString, true, out icon ) )
				icon = eIcon.None;

			category = (eCategory)dict.getInt( "category", (int)eCategory.Uncategorized );
		}

		internal static FileBase create( IReadOnlyDictionary<string, object> dict )
		{
			bool folder = (bool)dict[ "isfolder" ];
			if( folder )
				return new FolderInfo( dict );
			return new FileInfo( dict );
		}

		internal static FileBase[] createAll( IReadOnlyDictionary<string, object> dict )
		{
			if( dict.lookup( "contents" ) is object[] contents )
				return contents.OfType<IReadOnlyDictionary<string, object>>().Select( FileBase.create ).ToArray();
			return null;
		}
	}

	/// <summary>Folder metadata, can have children inside.</summary>
	public sealed class FolderInfo: FileBase
	{
		/// <summary>Contained subfolders and files. Can be null.</summary>
		public readonly FileBase[] children = null;

		internal FolderInfo( IReadOnlyDictionary<string, object> dict ) : base( true, dict )
		{
			children = createAll( dict );
		}

		/// <summary>Returns a string that represents the current object.</summary>
		public override string ToString()
		{
			int subfolders = 0;
			int files = 0;
			foreach( var c in children.enumerate() )
			{
				if( c is FolderInfo )
					subfolders++;
				else if( c is FileInfo )
					files++;
			}
			return $"\"{ name }\", { subfolders } subfolders, { files } files";
		}
	}

	/// <summary>File metadata</summary>
	public sealed class FileInfo: FileBase
	{
		/// <summary>Length in bytes</summary>
		public readonly long length;
		/// <summary>MIME type, such as "image/jpeg"</summary>
		public readonly string contentType;
		/// <summary>64 bit integer representing hash of the contents of the file can be used to determine if two files are the same or to monitor file contents for changes</summary>
		public readonly ulong hash;

		internal FileInfo( IReadOnlyDictionary<string, object> dict ) : base( false, dict )
		{
			length = dict.getLong( "size" );
			contentType = (string)dict[ "contenttype" ];
			hash = Convert.ToUInt64( dict[ "hash" ] );
		}

		/// <summary>Returns a string that represents the current object.</summary>
		public override string ToString()
		{
			return $"\"{ name }\", { length } bytes";
		}
	}
}