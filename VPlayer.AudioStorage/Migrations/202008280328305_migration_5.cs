namespace VPlayer.AudioStorage.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class migration_5 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Playlists", "SongsInPlaylitsHashCode", c => c.Long());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Playlists", "SongsInPlaylitsHashCode");
        }
    }
}
