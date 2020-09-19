using Ninject;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using VCore.Modularity.Events;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.AudioStorage.Interfaces.Storage;
using Windows.Media.Playlists;
using Windows.UI.Xaml.Media.Animation;
using Logger;
using VCore.Annotations;
using VPlayer.AudioStorage.Repositories;
using Playlist = VPlayer.AudioStorage.DomainClasses.Playlist;

namespace VPlayer.AudioStorage.AudioDatabase
{
  public class AudioDatabaseManager : IStorageManager, IInitializable
  {
    #region Fields

    private readonly AudioInfoDownloader audioInfoDownloader;
    private readonly PlaylistsRepository playlistsRepository;
    private readonly AlbumsRepository albumsRepository;
    private readonly ArtistRepository artistRepository;
    private readonly ILogger logger;

    private Subject<ItemChanged> ItemChanged { get; } = new Subject<ItemChanged>();

    #endregion Fields

    #region Constructors

    public AudioDatabaseManager(
      AudioInfoDownloader audioInfoDownloader,
      PlaylistsRepository playlistsRepository,
      AlbumsRepository albumsRepository,
      ArtistRepository artistRepository,
      ILogger logger)
    {
      this.audioInfoDownloader = audioInfoDownloader ?? throw new ArgumentNullException(nameof(audioInfoDownloader));
      this.playlistsRepository = playlistsRepository ?? throw new ArgumentNullException(nameof(playlistsRepository));
      this.albumsRepository = albumsRepository ?? throw new ArgumentNullException(nameof(albumsRepository));
      this.artistRepository = artistRepository ?? throw new ArgumentNullException(nameof(artistRepository));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #endregion Constructors

    #region Properties

    public Subject<Unit> ActionIsDone { get; } = new Subject<Unit>();

    #endregion Properties

    #region Methods

    #region Initialize

    private IDisposable disposable;

    public void Initialize()
    {
      disposable = audioInfoDownloader.ItemUpdated.Subscribe(ItemUpdated);
      audioInfoDownloader.SubdirectoryLoaded += AudioInfoDownloader_SubdirectoryLoaded;

      DownloadAllNotYetDownloaded(false);
    }

    #endregion

    #region GetRepository

    public IQueryable<T> GetRepository<T>(DbContext dbContext = null) where T : class
    {
      if (dbContext == null)
        dbContext = new AudioDatabaseContext();

      IQueryable<T> query = dbContext.Set<T>();

      return query;
    }

    #endregion 

    #region StoreData

    private object storeBatton = new object();

    public void StoreData(AudioInfo audioInfo)
    {

      lock (storeBatton)
      {
        try
        {
          using (var context = new AudioDatabaseContext())
          {
            Artist artist = new Artist(audioInfo.Artist)
            {
              MusicBrainzId = audioInfo.ArtistMbid
            };

            artist = (from x in context.Artists where x.Name == artist.Name select x).SingleOrDefault();

            if (artist == null && audioInfo.Artist != null)
            {
              artist = new Artist(audioInfo.Artist)
              {
                MusicBrainzId = audioInfo.ArtistMbid
              };

              context.Artists.Add(artist);
              context.SaveChanges();

              logger.Log(Logger.MessageType.Success, $"New artist was added {artist.Name}");

              ItemChanged.OnNext(new ItemChanged()
              {
                Item = artist,
                Changed = Changed.Added
              });

              audioInfoDownloader.UpdateItem(artist);
            }

            Album album = new Album()
            {
              Name = audioInfo.Album,
              Artist = artist,
            };

            album = (from x in context.Albums
                     where x.Name == album.Name
                     where x.Artist.Name == album.Artist.Name
                     select x).SingleOrDefault();

            if (album == null && audioInfo.Album != null)
            {
              album = new Album()
              {
                Name = audioInfo.Album,
                Artist = artist
              };

              context.Albums.Add(album);
              context.SaveChanges();

              logger.Log(Logger.MessageType.Success, $"New album was added {album.Name}");

              ItemChanged.OnNext(new ItemChanged()
              {
                Item = album,
                Changed = Changed.Added
              });

              audioInfoDownloader.UpdateItem(album);
            }

            Song song = new Song(audioInfo.Title, album)
            {
              DiskLocation = audioInfo.DiskLocation
            };


            song = (from x in context.Songs
                    where song.Name == x.Name
                    where x.Album.Id == song.Album.Id
                    select x).SingleOrDefault();


            if (song == null)
            {
              song = new Song(audioInfo.Title, album)
              {
                DiskLocation = audioInfo.DiskLocation,
                Duration = audioInfo.Duration
              };

              if (string.IsNullOrEmpty(song.Name))
              {
                song.Name = song.DiskLocation.Split('\\').Last();
              }

              context.Songs.Add(song);
              context.SaveChanges();

              ItemChanged.OnNext(new ItemChanged()
              {
                Item = song,
                Changed = Changed.Added
              });

              logger.Log(Logger.MessageType.Success, $"New song was added {song.Name}");
            }
          }
        }
        catch (Exception ex)
        {
          if (ex.InnerException != null)
          {
            if (ex.InnerException.InnerException != null)
            {
              logger.Log(Logger.MessageType.Error, $"{ex.InnerException.InnerException.Message}");
            }
            else
              logger.Log(Logger.MessageType.Error, $"{ex.InnerException.Message}");
          }
          else
          {
            logger.Log(Logger.MessageType.Error, $"{ex.Message}");
          }
        }
      }
    }

    public Task<bool> StoreData(IEnumerable<string> audioPath)
    {
      return Task.Run(async () =>
      {
        try
        {
          bool result = true;

          foreach (var audio in audioPath)
          {
            result = result && await StoreData(audio);
          }

          return result;
        }
        catch (Exception ex)
        {
          throw;
        }
        finally
        {
          ActionIsDone.OnNext(Unit.Default);
        }
      });
    }

    public async Task<bool> StoreData(string audioPath)
    {
      // get the file attributes for file or directory
      FileAttributes attr = File.GetAttributes(audioPath);

      //detect whether its a directory or file
      if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
      {
        audioInfoDownloader.GetAudioInfosFromDirectory(audioPath, true);
      }
      else
      {
        var audioInfo = audioInfoDownloader.GetAudioInfo(audioPath);

        if (audioInfo.Artist == null)
        {
        }

        StoreData(audioInfo);
      }

      return true;
    }


    private void AudioInfoDownloader_SubdirectoryLoaded(object sender, List<AudioInfo> e)
    {
      if (e?.Count > 0)
      {
        Task.Run(() =>
        {
          StoreData(e);
        });
      }
    }

    public void StoreData(List<AudioInfo> audioInfos)
    {
      try
      {
        foreach (var audioInfo in audioInfos)
        {
          if (audioInfo != null)
            StoreData(audioInfo);
        }
      }
      catch (Exception ex)
      {
        logger.Log(Logger.MessageType.Error, $"{ex.Message}");
      }
      finally
      {
        ActionIsDone.OnNext(Unit.Default);
      }
    }



    #region Playlist methods

    public bool StoreData(Playlist model, out Playlist entityModel)
    {
      var playlist = playlistsRepository.Context.Playlists.SingleOrDefault(x => x.SongsInPlaylitsHashCode == model.SongsInPlaylitsHashCode);

      if (playlist == null)
      {
        playlistsRepository.Add(model);
        playlistsRepository.Save();

        ItemChanged.OnNext(new ItemChanged()
        {
          Item = model,
          Changed = Changed.Added
        });

        ActionIsDone.OnNext(Unit.Default);

        entityModel = model;
        return true;
      }
      else
      {
        entityModel = playlist;
        return false;
      }

    }

    public void UpdateData(Playlist model)
    {
      var playlist = playlistsRepository.Context.Playlists.SingleOrDefault(x => x.Id == model.Id);

      if (playlist != null)
      {
        playlist.Update(model);

        playlistsRepository.Save();

        ItemChanged.OnNext(new ItemChanged()
        {
          Item = model,
          Changed = Changed.Updated
        });
      }
    }

    #endregion

    #endregion StoreData

    #region ClearStorage

    public Task ClearStorage()
    {
      return Task.Run(async () =>
      {
        using (var context = new AudioDatabaseContext())
        {
          try
          {

            await context.Database.ExecuteSqlCommandAsync("DELETE FROM PlaylistSongs");
            logger.Log(Logger.MessageType.Warning, "Table PlaylistSongs cleared succesfuly");

            var playlists = context.Playlists.ToList();
            await context.Database.ExecuteSqlCommandAsync("DELETE FROM Playlists");
            logger.Log(Logger.MessageType.Warning, "Table Playlists cleared succesfuly");

            foreach (var playlist in playlists)
            {
              var itemChange = new ItemChanged()
              {
                Changed = Changed.Removed,
                Item = playlist
              };

              ItemChanged.OnNext(itemChange);
            }

            ActionIsDone.OnNext(Unit.Default);

            await context.Database.ExecuteSqlCommandAsync("DELETE FROM Songs");
            logger.Log(Logger.MessageType.Warning, "Table Songs cleared succesfuly");

            var albums = context.Albums.ToList();
            await context.Database.ExecuteSqlCommandAsync("DELETE FROM Albums");
            logger.Log(Logger.MessageType.Warning, "Table Albums cleared succesfuly");

            foreach (var album in albums)
            {
              var itemChange = new ItemChanged()
              {
                Changed = Changed.Removed,
                Item = album
              };

              ItemChanged.OnNext(itemChange);
            }

            ActionIsDone.OnNext(Unit.Default);

            var artists = context.Artists.ToList();

            await context.Database.ExecuteSqlCommandAsync("DELETE FROM Artists");
            logger.Log(Logger.MessageType.Warning, "Table Artists cleared succesfuly");

            foreach (var artist in artists)
            {
              ItemChanged.OnNext(new ItemChanged()
              {
                Changed = Changed.Removed,
                Item = artist
              });
            }

            ActionIsDone.OnNext(Unit.Default);

          }
          catch (Exception ex)
          {
            logger.Log(Logger.MessageType.Inform, ex.Message);
          }
        }
      });
    }

    #endregion ClearStorage

    #region UpdateItem

    public void ItemUpdated(dynamic item)
    {
      Task.Run(() =>
      {
        if (item != null)
        {
          if (item is Album album)
          {
            UpdateAlbum(album);
          }
          else
          {
            UpdateEntity(item);
          }
        }
      });
    }

    #endregion

    #region UpdateAlbum

    private static object batton = new object();
    public void UpdateAlbum(Album album)
    {
      lock (batton)
      {
        using (var context = new AudioDatabaseContext())
        {

          try
          {
            var originalAlbum =
              (from x in context.Albums.Include(x => x.Artist)
               where x.MusicBrainzId != null
               where x.MusicBrainzId == album.MusicBrainzId
               select x).Include(x => x.Songs).Include(x => x.Artist).SingleOrDefault();

            //Update is first time
            if (originalAlbum == null)
            {
              var albums = context.Albums.Include(x => x.Songs).Include(x => x.Artist).ToList();

              originalAlbum = (from x in albums
                               where x.Id == album.Id
                               select x).SingleOrDefault();

              //Album could be deleted from storage
              if (originalAlbum != null)
              {
                originalAlbum.Update(album);

                var duplicates = (from x in albums
                                  where x.Name == originalAlbum.Name
                                  where x.Artist.Name == originalAlbum.Artist.Name
                                  group x by x.Name
                  into a
                                  where a.Count() > 1
                                  select a.ToList()).SingleOrDefault();

                if (duplicates == null)
                {
                  context.SaveChanges();
                  logger.Log(Logger.MessageType.Success,
                    $"Album was updated in database {album.Name}");

                  ItemChanged.OnNext(new ItemChanged()
                  {
                    Changed = Changed.Updated,
                    Item = originalAlbum
                  });
                }
                else
                {
                  Album originalCopy = null;
                  int oldId = duplicates[0].Id;

                  for (int i = 1; i < duplicates.Count; i++)
                  {
                    originalCopy = CombineAlbums(duplicates[0], duplicates[i], context);
                  }

                  if (originalCopy != null)
                  {
                    originalCopy.Id = oldId;
                    ItemChanged.OnNext(new ItemChanged()
                    {
                      Changed = Changed.Updated,
                      Item = originalCopy
                    });
                  }
                }
              }
              else
              {
                originalAlbum =
                  (from x in context.Albums
                   where x.Name == album.Name
                   where x.Artist.Name == album.Artist.Name
                   select x).Include(x => x.Songs).Include(x => x.Artist).SingleOrDefault();

                if (originalAlbum != null)
                  CombineAlbums(originalAlbum, album, context);
                else
                  ;
              }

              if (originalAlbum == null || originalAlbum.Artist == null)
              {
                return;
              }

              if (originalAlbum.Artist.ArtistCover == null &&
                  originalAlbum.Artist.AlbumIdCover == null &&
                  originalAlbum.AlbumFrontCoverFilePath != null)
              {
                originalAlbum.Artist.AlbumIdCover = originalAlbum.Id;
                context.SaveChanges();

                ItemChanged.OnNext(new ItemChanged()
                {
                  Changed = Changed.Updated,
                  Item = originalAlbum.Artist
                });
              }
            }
            else
            {
              if (album.Songs == null)
              {
                var dbAlbum = context.Albums.Where(x => x.Id == album.Id).Include(x => x.Songs).Include(x => x.Artist).SingleOrDefault();



                if (dbAlbum == null)
                {
                  logger.Log(Logger.MessageType.Warning,
                    $"Failed to combine, album was removed from database {album.Name}");
                }
                else
                {
                  dbAlbum.Update(album);
                  CombineAlbums(originalAlbum, dbAlbum, context);
                }

              }
              else
                CombineAlbums(originalAlbum, album, context);
            }
          }
          catch (Exception ex)
          {
            logger.Log(ex);
          }
        }
      }
    }

    #endregion

    #region CombineAlbums

    private Album CombineAlbums(Album originalAlbum, Album albumToCombine, AudioDatabaseContext context)
    {
      try
      {

        var songsToAdd =
          (from x in albumToCombine.Songs
           where originalAlbum.Songs.All(y => y.Name != x.Name)
           select x)
          .ToList();

        originalAlbum.Songs.AddRange(songsToAdd);

        context.Albums.Remove(albumToCombine);

        context.SaveChanges();

        ItemChanged.OnNext(new ItemChanged()
        {
          Changed = Changed.Removed,
          Item = albumToCombine
        });

        ItemChanged.OnNext(new ItemChanged()
        {
          Changed = Changed.Updated,
          Item = originalAlbum
        });

        logger.Log(Logger.MessageType.Warning, $"Combining album {albumToCombine.Name} to {originalAlbum.Name}");

        ActionIsDone.OnNext(Unit.Default);

        return originalAlbum;
      }
      catch (Exception ex)
      {
        logger.Log(ex);
        return null;
      }
    }

    #endregion CombineAlbums

    #region RewriteEntity

    public void RewriteEntity<T>(T entity) where T : class, IEntity
    {
      using (var context = new AudioDatabaseContext())
      {
        var foundEntity = GetRepository<T>(context).SingleOrDefault(x => x.Id == entity.Id);

        if (foundEntity != null)
        {
          context.Entry(foundEntity).CurrentValues.SetValues(entity);

          context.SaveChanges();

          logger.Log(Logger.MessageType.Success, $"Entity was updated {entity}");

          ItemChanged.OnNext(new ItemChanged()
          {
            Item = foundEntity,
            Changed = Changed.Updated
          });
        }
      }
    }

    #endregion UpdateEntity

    #region UpdateEntity

    public Task<bool> UpdateEntity<TEntity>(TEntity newVersion) where TEntity : class, IEntity, IUpdateable<TEntity>
    {
      return Task.Run(() =>
      {
        using (var context = new AudioDatabaseContext())
        {
          var foundEntity = GetRepository<TEntity>(context).SingleOrDefault(x => x.Id == newVersion.Id);

          if (foundEntity != null)
          {
            foundEntity.Update(newVersion);

            context.SaveChanges();

            logger.Log(Logger.MessageType.Success, $"Entity was updated {newVersion}");

            ItemChanged.OnNext(new ItemChanged()
            {
              Item = foundEntity,
              Changed = Changed.Updated
            });

            return true;
          }

          return false;
        }
      });
    }

    #endregion

    #region DeletePlaylist

    //TODO: GetRepository<TEntity>(context) toto spravit genericky pre repository patern
    //TODO: Cely repository prerobit
    public Task DeletePlaylist(Playlist playlist)
    {
      return Task.Run(() =>
      {
        try
        {
          using (var context = new AudioDatabaseContext())
          {
            var entityPlaylist = context.Playlists.SingleOrDefault(x => x.Id == playlist.Id);

            if (entityPlaylist != null)
            {
              context.Playlists.Remove(entityPlaylist);
              context.SaveChanges();

              ItemChanged.OnNext(new ItemChanged()
              {
                Item = playlist,
                Changed = Changed.Removed
              });
            }
          }
        }
        catch (Exception ex)
        {
          logger.Log(ex);
        }
      });
    }

    #endregion

    #region DownloadAllNotYetDownloaded

    public Task DownloadAllNotYetDownloaded(bool tryDownloadBroken = false)
    {
      return Task.Run(() =>
      {
        var notUpdatedAlbums = albumsRepository.Entities.Where(x => x.InfoDownloadStatus == InfoDownloadStatus.Waiting
                                                                    || x.InfoDownloadStatus == InfoDownloadStatus.Downloading)
          .Include(x => x.Artist).OrderByDescending(x => x.InfoDownloadStatus).ToList();

        foreach (var album in notUpdatedAlbums)
        {
          audioInfoDownloader.UpdateItem(album);
        }

        if (tryDownloadBroken)
        {
          var brokenAlbums = albumsRepository.Entities.Where(x => x.InfoDownloadStatus == InfoDownloadStatus.Failed
                                                                  || x.InfoDownloadStatus == InfoDownloadStatus.UnableToFind)
            .Include(x => x.Artist);

          foreach (var album in brokenAlbums)
          {
            audioInfoDownloader.UpdateItem(album);
          }
        }
      });
    }

    #endregion

    #region SubscribeToItemChange

    public IDisposable SubscribeToItemChange<TModel>(Action<ItemChanged<TModel>> observer)
    {
      return ItemChanged.Where(x => x.Item.GetType() == typeof(TModel))
        .Select(x => new ItemChanged<TModel>((TModel)x.Item, x.Changed)).Subscribe(observer);
    }

    #endregion

    #region PushAction

    public void PushAction(ItemChanged itemChanged)
    {
      ItemChanged.OnNext(itemChanged);
    } 

    #endregion

    #region Dispose

    public void Dispose()
    {
      ItemChanged?.Dispose();
      ActionIsDone?.Dispose();
      disposable?.Dispose();
    }

    #endregion

    #endregion

  }
}