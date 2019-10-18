namespace VPlayer.AudioStorage.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Artists", "Created", c => c.DateTime());
            AddColumn("dbo.Artists", "Modified", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Artists", "Modified");
            DropColumn("dbo.Artists", "Created");
        }
    }
}
