using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Migrations;

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

    public override int SaveChanges()
    {
      OnBeforeSaving();

      return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync()
    {
      OnBeforeSaving();

      return base.SaveChangesAsync();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
      OnBeforeSaving();

      return base.SaveChangesAsync(cancellationToken);
    }

    private void OnBeforeSaving()
    {
      var entries = ChangeTracker.Entries();
      foreach (var entry in entries)
      {
        if (entry.Entity is ITrackable trackable)
        {
          var now = DateTime.UtcNow;

          switch (entry.State)
          {
            case EntityState.Modified:
              trackable.Modified = now;
              break;

            case EntityState.Added:
              trackable.Created = now;
              break;
          }
        }
      }
    }
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