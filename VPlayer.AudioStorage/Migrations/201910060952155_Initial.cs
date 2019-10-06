namespace VPlayer.AudioStorage.Migrations
{
  using System.Data.Entity.Migrations;

  public partial class Initial : DbMigration
  {
    #region Methods

    public override void Down()
    {
      DropForeignKey("dbo.Songs", "Album_Id", "dbo.Albums");
      DropForeignKey("dbo.Albums", "Artist_Id", "dbo.Artists");
      DropIndex("dbo.Songs", new[] { "Album_Id" });
      DropIndex("dbo.Albums", new[] { "Artist_Id" });
      DropTable("dbo.Songs");
      DropTable("dbo.Artists");
      DropTable("dbo.Albums");
    }

    public override void Up()
    {
      CreateTable(
          "dbo.Albums",
          c => new
          {
            Id = c.Int(nullable: false, identity: true),
            AlbumFrontCoverBLOB = c.Binary(),
            AlbumFrontCoverURI = c.String(),
            MusicBrainzId = c.String(),
            Name = c.String(),
            ReleaseDate = c.String(),
            Artist_Id = c.Int(),
          })
        .PrimaryKey(t => t.Id)
        .ForeignKey("dbo.Artists", t => t.Artist_Id)
        .Index(t => t.Artist_Id);

      CreateTable(
          "dbo.Artists",
          c => new
          {
            Id = c.Int(nullable: false, identity: true),
            Name = c.String(),
            ArtistCover = c.Binary(),
            AlbumIdCover = c.Int(),
            MusicBrainzId = c.String(),
          })
        .PrimaryKey(t => t.Id);

      CreateTable(
          "dbo.Songs",
          c => new
          {
            Id = c.Int(nullable: false, identity: true),
            Name = c.String(),
            DiskLocation = c.String(),
            Length = c.Int(nullable: false),
            MusicBrainzId = c.String(),
            Duration = c.Int(nullable: false),
            Album_Id = c.Int(),
          })
        .PrimaryKey(t => t.Id)
        .ForeignKey("dbo.Albums", t => t.Album_Id)
        .Index(t => t.Album_Id);
    }

    #endregion Methods
  }
}