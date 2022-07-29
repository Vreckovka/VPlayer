using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Logger;
using Microsoft.EntityFrameworkCore;
using Ninject;
using VCore.Standard;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Managers;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.FolderStructure;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.AudioStorage.DomainClasses.UPnP;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Scrappers.CSFD.Domain;
using VPlayer.IPTV.ViewModels;

namespace VPlayer.AudioStorage.AudioDatabase
{
  public class AudioDatabaseContext : DbContext
  {
    private readonly ILogger logger;

    private readonly IWindowManager windowManager;
    //add-migration migration_ -ConnectionString "Data Source=C:\Users\Roman Pecho\AppData\Roaming\VPlayer\VPlayerDatabase.db;Version=3;" -connectionProvider "System.Data.SQLite.EF6"

    //STACI IBA add-migration MIGRATIONNAME 

    #region Properties

    public DbSet<VideoItem> VideoItems { get; set; }
    public DbSet<SoundItem> SoundItems { get; set; }
    public DbSet<SoundFileInfo> SoundFileInfos { get; set; }


    public DbSet<Album> Albums { get; set; }
    public DbSet<Artist> Artists { get; set; }
    public DbSet<Song> Songs { get; set; }
    public DbSet<PlaylistSoundItem> PlaylistSongs { get; set; }
    public DbSet<SoundItemFilePlaylist> SongPlaylists { get; set; }


    public DbSet<TvShow> TvShows { get; set; }
    public DbSet<TvShowSeason> TvShowsSeasons { get; set; }
    public DbSet<TvShowEpisode> TvShowEpisodes { get; set; }
    public DbSet<PlaylistVideoItem> PlaylistsTvShowEpisode { get; set; }
    public DbSet<VideoFilePlaylist> TvShowPlaylists { get; set; }



    public DbSet<UPnPMediaServer> UPnPMediaServers { get; set; }
    public DbSet<UPnPMediaRenderer> UPnPMediaRenderers { get; set; }
    public DbSet<UPnPDevice> UPnPDevices { get; set; }
    public DbSet<UPnPService> UPnPServices { get; set; }



    public DbSet<TvSource> TvSources { get; set; }
    public DbSet<TvChannelGroup> TvChannelGroups { get; set; }
    public DbSet<TvChannelGroupItem> TvChannelGroupItems { get; set; }
    public DbSet<TvChannel> TvChannels { get; set; }
    public DbSet<TvPlaylist> TvPlaylists { get; set; }



    public DbSet<ItemBookmark> Bookmarks { get; set; }



    //public DbSet<CSFDItem> CSFDItems { get; set; }
    //public DbSet<CSFDTVShowSeasonEpisodeEntity> CSFDTVShowSeasonEpisodes { get; set; }

    #endregion Properties


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VPlayer");

      if (!Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
      }

      optionsBuilder.UseSqlite($"Data Source={directory}\\VPlayerDatabase.db;");

      base.OnConfiguring(optionsBuilder);
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      modelBuilder.Entity<SoundItemFilePlaylist>()
        .HasIndex(nameof(SoundItemFilePlaylist.HashCode), nameof(SoundItemFilePlaylist.IsUserCreated))
        .IsUnique();

      modelBuilder.Entity<VideoFilePlaylist>()
        .HasIndex(nameof(SoundItemFilePlaylist.HashCode), nameof(SoundItemFilePlaylist.IsUserCreated))
        .IsUnique();
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