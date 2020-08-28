namespace VPlayer.AudioStorage.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class migration_6 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Playlists", "SongCount", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Playlists", "SongCount");
        }
    }
}
