namespace VPlayer.AudioStorage.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Albums",
                c => new
                    {
                        AlbumId = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        ReleaseDate = c.String(),
                        MusicBrainzId = c.String(),
                        AlbumFrontCoverURI = c.String(),
                        AlbumFrontCoverBLOB = c.Binary(),
                        Hash = c.String(maxLength: 64),
                        Artist_ArtistId = c.Int(),
                    })
                .PrimaryKey(t => t.AlbumId)
                .ForeignKey("dbo.Artists", t => t.Artist_ArtistId)
                .Index(t => t.Hash, unique: true, name: "Hash_Album")
                .Index(t => t.Artist_ArtistId);
            
            CreateTable(
                "dbo.Artists",
                c => new
                    {
                        ArtistId = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        MusicBrainzId = c.String(),
                        Hash = c.String(maxLength: 64),
                    })
                .PrimaryKey(t => t.ArtistId)
                .Index(t => t.Hash, unique: true, name: "Hash_Artist");
            
            CreateTable(
                "dbo.Songs",
                c => new
                    {
                        SongId = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        DiskLocation = c.String(),
                        Length = c.Int(nullable: false),
                        MusicBrainzId = c.String(),
                        Hash = c.String(maxLength: 64),
                        Album_AlbumId = c.Int(),
                        Artist_ArtistId = c.Int(),
                        Genre_GenreId = c.Int(),
                    })
                .PrimaryKey(t => t.SongId)
                .ForeignKey("dbo.Albums", t => t.Album_AlbumId)
                .ForeignKey("dbo.Artists", t => t.Artist_ArtistId)
                .ForeignKey("dbo.Genres", t => t.Genre_GenreId)
                .Index(t => t.Hash, unique: true, name: "Hash_Song")
                .Index(t => t.Album_AlbumId)
                .Index(t => t.Artist_ArtistId)
                .Index(t => t.Genre_GenreId);
            
            CreateTable(
                "dbo.Genres",
                c => new
                    {
                        GenreId = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Hash = c.String(maxLength: 64),
                    })
                .PrimaryKey(t => t.GenreId)
                .Index(t => t.Hash, unique: true, name: "Hash_Genre");
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Songs", "Genre_GenreId", "dbo.Genres");
            DropForeignKey("dbo.Songs", "Artist_ArtistId", "dbo.Artists");
            DropForeignKey("dbo.Songs", "Album_AlbumId", "dbo.Albums");
            DropForeignKey("dbo.Albums", "Artist_ArtistId", "dbo.Artists");
            DropIndex("dbo.Genres", "Hash_Genre");
            DropIndex("dbo.Songs", new[] { "Genre_GenreId" });
            DropIndex("dbo.Songs", new[] { "Artist_ArtistId" });
            DropIndex("dbo.Songs", new[] { "Album_AlbumId" });
            DropIndex("dbo.Songs", "Hash_Song");
            DropIndex("dbo.Artists", "Hash_Artist");
            DropIndex("dbo.Albums", new[] { "Artist_ArtistId" });
            DropIndex("dbo.Albums", "Hash_Album");
            DropTable("dbo.Genres");
            DropTable("dbo.Songs");
            DropTable("dbo.Artists");
            DropTable("dbo.Albums");
        }
    }
}
