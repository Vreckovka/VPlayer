using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.IO;
using VPlayer.Core.DomainClasses;

namespace VPlayer.AudioStorage.AudioDatabase
{
  public class AudioDatabaseContext : DbContext
  {
    #region Constructors

    public AudioDatabaseContext()
    {
      this.Configuration.LazyLoadingEnabled = false;
      this.Configuration.ProxyCreationEnabled = false;

      var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VPlayer");
      if (!Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
      }

      var path = Path.Combine(directory, "VPlayer.AudioDatabase.mdf");

      Database.Connection.ConnectionString =
        "Data Source=(LocalDB)\\MSSQLLocalDB;" +
        $"AttachDbFilename={path};" +
        "Integrated Security=True;" +
        "MultipleActiveResultSets=true";

      //Database.SetInitializer(new MigrateDatabaseToLatestVersion<AudioDatabaseContext, Configuration>());
    }

    #endregion Constructors

    #region Properties

    public DbSet<Album> Albums { get; set; }
    public DbSet<Artist> Artists { get; set; }
    public DbSet<Song> Songs { get; set; }

    #endregion Properties
  }

  public partial class RenameKey : DbMigration
  {
    #region Methods

    public override void Up()
    {
      RenameColumn("dbo.Album", "AlbumId", "Id");
    }

    #endregion Methods
  }
}