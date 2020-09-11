namespace VPlayer.AudioStorage.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class migration_3 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Songs", "Chartlyrics_Lyric", c => c.String(maxLength: 2147483647));
            AddColumn("dbo.Songs", "Chartlyrics_LyricId", c => c.String(maxLength: 2147483647));
            AddColumn("dbo.Songs", "Chartlyrics_LyricCheckSum", c => c.String(maxLength: 2147483647));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Songs", "Chartlyrics_LyricCheckSum");
            DropColumn("dbo.Songs", "Chartlyrics_LyricId");
            DropColumn("dbo.Songs", "Chartlyrics_Lyric");
        }
    }
}
