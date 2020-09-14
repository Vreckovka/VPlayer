namespace VPlayer.AudioStorage.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class migration_31 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Songs", "LRCLyrics", c => c.String(maxLength: 2147483647));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Songs", "LRCLyrics");
        }
    }
}
