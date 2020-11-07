namespace VPlayer.AudioStorage.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class migration_ : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Albums",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        AlbumFrontCoverFilePath = c.String(maxLength: 2147483647),
                        AlbumFrontCoverURI = c.String(maxLength: 2147483647),
                        MusicBrainzId = c.String(maxLength: 2147483647),
                        Name = c.String(maxLength: 2147483647),
                        ReleaseDate = c.String(maxLength: 2147483647),
                        InfoDownloadStatus = c.Int(nullable: false),
                        Artist_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Artists", t => t.Artist_Id)
                .Index(t => t.Artist_Id, name: "IX_Album_Artist_Id");
            
            CreateTable(
                "dbo.Artists",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        AlbumIdCover = c.Int(),
                        ArtistCover = c.String(maxLength: 2147483647),
                        MusicBrainzId = c.String(maxLength: 2147483647),
                        Name = c.String(maxLength: 2147483647),
                        InfoDownloadStatus = c.Int(nullable: false),
                        Created = c.DateTime(),
                        Modified = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Songs",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        DiskLocation = c.String(maxLength: 2147483647),
                        Duration = c.Int(nullable: false),
                        Length = c.Int(nullable: false),
                        MusicBrainzId = c.String(maxLength: 2147483647),
                        Name = c.String(maxLength: 2147483647),
                        Chartlyrics_Lyric = c.String(maxLength: 2147483647),
                        Chartlyrics_LyricId = c.String(maxLength: 2147483647),
                        Chartlyrics_LyricCheckSum = c.String(maxLength: 2147483647),
                        LRCLyrics = c.String(maxLength: 2147483647),
                        IsFavorite = c.Boolean(nullable: false),
                        Album_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Albums", t => t.Album_Id)
                .Index(t => t.Album_Id, name: "IX_Song_Album_Id");
            
            CreateTable(
                "dbo.Playlists",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(maxLength: 2147483647),
                        SongsInPlaylitsHashCode = c.Long(),
                        SongCount = c.Int(),
                        IsReapting = c.Boolean(nullable: false),
                        IsShuffle = c.Boolean(nullable: false),
                        LastSongElapsedTime = c.Double(nullable: false, storeType: "real"),
                        LastSongIndex = c.Int(nullable: false),
                        IsUserCreated = c.Boolean(nullable: false),
                        LastPlayed = c.DateTime(nullable: false),
                        Created = c.DateTime(),
                        Modified = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.PlaylistSongs",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        OrderInPlaylist = c.Int(nullable: false),
                        IdSong = c.Int(nullable: false),
                        Created = c.DateTime(),
                        Modified = c.DateTime(),
                        Playlist_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Songs", t => t.IdSong, cascadeDelete: true)
                .ForeignKey("dbo.Playlists", t => t.Playlist_Id)
                .Index(t => t.IdSong, name: "IX_PlaylistSong_IdSong")
                .Index(t => t.Playlist_Id, name: "IX_PlaylistSong_Playlist_Id");
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PlaylistSongs", "Playlist_Id", "dbo.Playlists");
            DropForeignKey("dbo.PlaylistSongs", "IdSong", "dbo.Songs");
            DropForeignKey("dbo.Songs", "Album_Id", "dbo.Albums");
            DropForeignKey("dbo.Albums", "Artist_Id", "dbo.Artists");
            DropIndex("dbo.PlaylistSongs", "IX_PlaylistSong_Playlist_Id");
            DropIndex("dbo.PlaylistSongs", "IX_PlaylistSong_IdSong");
            DropIndex("dbo.Songs", "IX_Song_Album_Id");
            DropIndex("dbo.Albums", "IX_Album_Artist_Id");
            DropTable("dbo.PlaylistSongs");
            DropTable("dbo.Playlists");
            DropTable("dbo.Songs");
            DropTable("dbo.Artists");
            DropTable("dbo.Albums");
        }
    }
}
