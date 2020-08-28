namespace VPlayer.AudioStorage.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class migration_4 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Playlists",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(maxLength: 2147483647),
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
            DropIndex("dbo.PlaylistSongs", "IX_PlaylistSong_Playlist_Id");
            DropIndex("dbo.PlaylistSongs", "IX_PlaylistSong_IdSong");
            DropTable("dbo.PlaylistSongs");
            DropTable("dbo.Playlists");
        }
    }
}
