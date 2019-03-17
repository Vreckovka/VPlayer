namespace VPlayer.LocalMusicDatabase.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
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
                        AlbumFrontCoverURI = c.String(),
                        Artist_ArtistId = c.Int(),
                    })
                .PrimaryKey(t => t.AlbumId)
                .ForeignKey("dbo.Artists", t => t.Artist_ArtistId)
                .Index(t => t.Artist_ArtistId);
            
            CreateTable(
                "dbo.Artists",
                c => new
                    {
                        ArtistId = c.Int(nullable: false, identity: true),
                        MusicBrainzId = c.String(),
                        Name = c.String(),
                    })
                .PrimaryKey(t => t.ArtistId);
            
            CreateTable(
                "dbo.Songs",
                c => new
                    {
                        SongId = c.Int(nullable: false, identity: true),
                        DiskLocation = c.String(),
                        Title = c.String(),
                        Length = c.Int(nullable: false),
                        Album_AlbumId = c.Int(),
                    })
                .PrimaryKey(t => t.SongId)
                .ForeignKey("dbo.Albums", t => t.Album_AlbumId)
                .Index(t => t.Album_AlbumId);
            
            CreateTable(
                "dbo.Genres",
                c => new
                    {
                        GenreId = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                    })
                .PrimaryKey(t => t.GenreId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Songs", "Album_AlbumId", "dbo.Albums");
            DropForeignKey("dbo.Albums", "Artist_ArtistId", "dbo.Artists");
            DropIndex("dbo.Songs", new[] { "Album_AlbumId" });
            DropIndex("dbo.Albums", new[] { "Artist_ArtistId" });
            DropTable("dbo.Genres");
            DropTable("dbo.Songs");
            DropTable("dbo.Artists");
            DropTable("dbo.Albums");
        }
    }
}
