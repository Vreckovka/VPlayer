﻿namespace VPlayer.AudioStorage.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class migration_1 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Albums",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        AlbumFrontCoverBLOB = c.Binary(),
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
                        ArtistCover = c.Binary(),
                        MusicBrainzId = c.String(maxLength: 2147483647),
                        Name = c.String(maxLength: 2147483647),
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
                        Album_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Albums", t => t.Album_Id)
                .Index(t => t.Album_Id, name: "IX_Song_Album_Id");
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Songs", "Album_Id", "dbo.Albums");
            DropForeignKey("dbo.Albums", "Artist_Id", "dbo.Artists");
            DropIndex("dbo.Songs", "IX_Song_Album_Id");
            DropIndex("dbo.Albums", "IX_Album_Artist_Id");
            DropTable("dbo.Songs");
            DropTable("dbo.Artists");
            DropTable("dbo.Albums");
        }
    }
}