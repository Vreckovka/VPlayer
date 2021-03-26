using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Logger;
using Microsoft.EntityFrameworkCore;
using Ninject;
using VCore.Annotations;
using VCore.Standard;
using VCore.WPF.Managers;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;

namespace VPlayer.AudioStorage.AudioDatabase
{
  public class AudioDatabaseContext : DbContext
  {
    private readonly ILogger logger;

    private readonly IWindowManager windowManager;
    //add-migration migration_ -ConnectionString "Data Source=C:\Users\Roman Pecho\AppData\Roaming\VPlayer\VPlayerDatabase.db;Version=3;" -connectionProvider "System.Data.SQLite.EF6"
    
    //STACI IBA add-migration MIGRATIONNAME 

    #region Properties

    public DbSet<Album> Albums { get; set; }
    public DbSet<Artist> Artists { get; set; }
    public DbSet<Song> Songs { get; set; }
    public DbSet<SongsPlaylist> SongPlaylists { get; set; }
    public DbSet<PlaylistSong> PlaylistSongs { get; set; }


    public DbSet<TvShow> TvShows { get; set; }
    public DbSet<TvShowSeason> TvShowsSeasons { get; set; }
    public DbSet<TvShowEpisode> TvShowEpisodes { get; set; }
    public DbSet<PlaylistTvShowEpisode> PlaylistsTvShowEpisode { get; set; }
    public DbSet<TvShowPlaylist> TvShowPlaylists { get; set; }

    #endregion Properties

    #region Constructors

    public AudioDatabaseContext() : base()
    {
      var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VPlayer");
      if (!Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
      }

    }

    #endregion

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      base.OnConfiguring(optionsBuilder);

      optionsBuilder.UseSqlite("Data Source=C:\\Users\\Roman Pecho\\AppData\\Roaming\\VPlayer\\VPlayerDatabase.db;");
    }
  

    public override int SaveChanges()
    {
      OnBeforeSaving();

      return base.SaveChanges();
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
}