using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SQLite.CodeFirst;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;

namespace VPlayer.AudioStorage.AudioDatabase
{
  public class AudioDatabaseContext : DbContext
  {
    //add-migration migration_ -ConnectionString "Data Source=C:\Users\Roman Pecho\AppData\Roaming\VPlayer\VPlayerDatabase.db;Version=3;" -connectionProvider "System.Data.SQLite.EF6"

    //https://stackoverflow.com/questions/56226978/no-migrationsqlgenerator-found-for-provider-system-data-sqlite
    //Zevraj nepodporuje migracie

    #region Properties

    public DbSet<Album> Albums { get; set; }
    public DbSet<Artist> Artists { get; set; }
    public DbSet<Song> Songs { get; set; }
    public DbSet<SongsPlaylist> SongPlaylists { get; set; }
    public DbSet<PlaylistSong> PlaylistSongs { get; set; }


    public DbSet<TvShow> TvShows { get; set; }
    public DbSet<TvShowEpisode> TvShowEpisodes { get; set; }
    public DbSet<PlaylistTvShowEpisode> PlaylistsTvShowEpisode { get; set; }
    public DbSet<TvShowPlaylist> TvShowPlaylists { get; set; }

    #endregion Properties

    #region Constructors

    public AudioDatabaseContext() : base("DataContext")
    {
      this.Configuration.LazyLoadingEnabled = false;
      this.Configuration.ProxyCreationEnabled = false;

      var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VPlayer");
      if (!Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
      }

      var _ = System.Data.Entity.SqlServer.SqlProviderServices.Instance;
    }

    #endregion Constructors

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
      var sqliteConnectionInitializer = new SqliteCreateDatabaseIfNotExists<AudioDatabaseContext>(modelBuilder);
      
      Database.SetInitializer(sqliteConnectionInitializer);
    }

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