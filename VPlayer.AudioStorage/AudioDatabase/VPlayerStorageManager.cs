using Ninject;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.AudioStorage.Interfaces.Storage;
using Logger;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query;
using VCore;
using VCore.Standard;
using VCore.Standard.Helpers;
using VCore.Standard.Modularity.Interfaces;
using VCore.Standard.ViewModels.WindowsFile;
using VCore.WPF.Modularity.Events;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.AudioStorage.Repositories;

namespace VPlayer.AudioStorage.AudioDatabase
{
  public class VPlayerStorageManager : IStorageManager, IInitializable
  {
    #region Fields

    private readonly AudioInfoDownloader audioInfoDownloader;
    private readonly ILogger logger;

    public ReplaySubject<IItemChanged> itemChanged { get; } = new ReplaySubject<IItemChanged>(1);

    #endregion Fields

    #region Constructors

    public VPlayerStorageManager(
      AudioInfoDownloader audioInfoDownloader,
      ILogger logger)
    {
      this.audioInfoDownloader = audioInfoDownloader ?? throw new ArgumentNullException(nameof(audioInfoDownloader));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #endregion Constructors

    #region Properties

    public Subject<Unit> ActionIsDone { get; } = new Subject<Unit>();


    public IObservable<IItemChanged> OnItemChanged
    {
      get { return itemChanged.AsObservable(); }
    }


    #endregion Properties

    #region Methods

    #region Initialize

    private IDisposable disposable;

    public void Initialize()
    {
      disposable = audioInfoDownloader.ItemUpdated.Subscribe(ItemUpdated);
      audioInfoDownloader.SubdirectoryLoaded += AudioInfoDownloader_SubdirectoryLoaded;

      //DownloadAllNotYetDownloaded(false);
    }

    #endregion

    #region GetTempRepository

    public IQueryable<T> GetTempRepository<T>() where T : class
    {
      return new AudioDatabaseContext().Set<T>().AsNoTracking();
    }

    #endregion

    #region GetRepository

    public DbSet<T> GetRepository<T>(DbContext dbContext) where T : class
    {
      return dbContext.Set<T>();
    }

    #endregion

    #region GetNormalizedName

    public static string GetNormalizedName(string name)
    {
      if (string.IsNullOrEmpty(name))
      {
        return name;
      }

      return new String(name.RemoveDiacritics().ToLower().Where(x => Char.IsLetterOrDigit(x)).ToArray());
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
            Artist newArtist = new Artist(audioInfo.Artist)
            {
              MusicBrainzId = audioInfo.ArtistMbid,
              NormalizedName = GetNormalizedName(audioInfo.Artist)
            };

            Artist artist = newArtist;

            artist = (from x in context.Artists where x.NormalizedName == artist.NormalizedName select x).SingleOrDefault();

            if (artist == null && audioInfo.Artist != null)
            {
              artist = newArtist;

              context.Artists.Add(artist);
              context.SaveChanges();

              logger.Log(Logger.MessageType.Success, $"New artist was added {artist.Name}");

              PublishItemChanged(artist, Changed.Added);

              audioInfoDownloader.UpdateItem(artist);
            }

            Album album = new Album()
            {
              Name = audioInfo.Album,
              Artist = artist,
              NormalizedName = GetNormalizedName(audioInfo.Album)
            };

            album = (from x in context.Albums
                     where x.NormalizedName == album.NormalizedName
                     where x.Artist.NormalizedName == album.Artist.NormalizedName
                     select x).SingleOrDefault();

            if (album == null && audioInfo.Album != null)
            {
              album = new Album()
              {
                Name = audioInfo.Album,
                Artist = artist,
                NormalizedName = GetNormalizedName(audioInfo.Album)
              };

              context.Albums.Add(album);
              context.SaveChanges();

              logger.Log(Logger.MessageType.Success, $"New album was added {album.Name}");

              PublishItemChanged(album, Changed.Added);

              audioInfoDownloader.UpdateItem(album);
            }


            var fileInfo = context.FileInfos.SingleOrDefault(x => x.Indentificator == audioInfo.DiskLocation);

            if (fileInfo == null)
            {
              fileInfo = new FileInfoEntity(audioInfo.DiskLocation, audioInfo.DiskLocation)
              {
                Indentificator = audioInfo.DiskLocation,
                Artist = audioInfo.Artist,
                Album = audioInfo.Album,
                Title = audioInfo.Title,
                Source = audioInfo.DiskLocation,
                Name = Path.GetFileName(audioInfo.DiskLocation),
                FullName = Path.GetFileName(audioInfo.DiskLocation)
              };
            }


            var soundItem = context.SoundItems.SingleOrDefault(x => x.FileInfoEntity == fileInfo);

            if (soundItem == null)
            {
              soundItem = new SoundItem()
              {
                Duration = audioInfo.Duration,
                NormalizedName = GetNormalizedName(audioInfo.Title),
              };
            }

            soundItem.FileInfoEntity = fileInfo;

            var song = context.Songs.SingleOrDefault(x => x.ItemModel == soundItem);

            if (song == null)
            {
              song = new Song(album)
              {
                ItemModel = soundItem
              };

              if (string.IsNullOrEmpty(song.ItemModel.Name))
              {
                song.ItemModel.FileInfoEntity.Name = song.ItemModel.FileInfoEntity.Name;
              }

              context.Songs.Add(song);
              context.SaveChanges();

              PublishItemChanged(song, Changed.Added);

              logger.Log(Logger.MessageType.Success, $"New song was added {song.Name}");
            }
            else
            {
              song.ItemModel.FileInfoEntity.Source = audioInfo.DiskLocation;

              PublishItemChanged(song);

              context.SaveChanges();

              logger.Log(Logger.MessageType.Success, $"Song was updated {song.Name}");
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


    #endregion StoreData

    #region CleanData

    public async Task CleanData()
    {
      await Task.Run(() =>
      {
        using (var context = new AudioDatabaseContext())
        {
          var albums = GetRepository<Album>(context).Where(x => x.Songs.Count == 0);

          foreach (var album in albums)
          {
            context.Entry(album).State = EntityState.Deleted;

            PublishItemChanged(album, Changed.Removed);
          }

          var result = context.SaveChanges();

          var artists = GetRepository<Artist>(context).Where(x => x.Albums.Count == 0);

          foreach (var artist in artists)
          {
            context.Entry(artist).State = EntityState.Deleted;
            PublishItemChanged(artist, Changed.Removed);
          }

          result = context.SaveChanges();
        }
      });

      //select Indentificator, count(*) pocet from FileInfos where Indentificator is not null  GROUP by Indentificator  having count(*) > 1 

      await Task.Run(() =>
      {
        using (var context = new AudioDatabaseContext())
        {
          var fileInfos = GetRepository<FileInfoEntity>(context)
            .ToList()
            .GroupBy(x => x.Indentificator).
            Where(x => x.Count() > 1).ToList();


          foreach (var fileInfo in fileInfos.SelectMany(x => x))
          {
            try
            {
              var fileType = ExtentionHelper.GetFileType(fileInfo.Extension);
              PlayableItem playableItem = null;
              List<DomainEntity> playlistItems = null;

              if (fileType == FileType.Sound)
              {
                playableItem = GetRepository<SoundItem>(context)
                  .SingleOrDefault(x => x.FileInfoEntity.Id == fileInfo.Id);

              }
              else if (fileType == FileType.Video)
              {
                playableItem = GetRepository<VideoItem>(context)
                  .SingleOrDefault(x => x.FileInfoEntity.Id == fileInfo.Id);
              }


              if (playableItem != null)
                context.Remove(playableItem);


              context.Remove(fileInfo);
              //context.SaveChanges();
            }
            catch (Exception ex)
            {
            }
          }

          var result = context.SaveChanges();

          var artists = GetRepository<Artist>(context).Where(x => x.Albums.Count == 0);

          foreach (var artist in artists)
          {
            context.Entry(artist).State = EntityState.Deleted;
            PublishItemChanged(artist, Changed.Removed);
          }

          result = context.SaveChanges();
        }
      });
    }

    #endregion

    #region ClearStorage

    public Task ClearStorage()
    {
      throw new NotImplementedException();
      //return Task.Run(async () =>
      //{
      //  using (var context = new AudioDatabaseContext())
      //  {
      //    try
      //    {

      //      await context.Database.ExecuteSqlCommandAsync("DELETE FROM PlaylistSongs");
      //      logger.Log(Logger.MessageType.Warning, "Table PlaylistSongs cleared succesfuly");

      //      var playlists = context.SongPlaylists.ToList();
      //      await context.Database.ExecuteSqlCommandAsync("DELETE FROM Playlists");
      //      logger.Log(Logger.MessageType.Warning, "Table Playlists cleared succesfuly");

      //      foreach (var playlist in playlists)
      //      {
      //        var itemChange = new ItemChanged()
      //        {
      //          Changed = Changed.Removed,
      //          Item = playlist
      //        };

      //        ItemChanged.OnNext(itemChange);
      //      }

      //      ActionIsDone.OnNext(Unit.Default);

      //      await context.Database.ExecuteSqlCommandAsync("DELETE FROM Songs");
      //      logger.Log(Logger.MessageType.Warning, "Table Songs cleared succesfuly");

      //      var albums = context.Albums.ToList();
      //      await context.Database.ExecuteSqlCommandAsync("DELETE FROM Albums");
      //      logger.Log(Logger.MessageType.Warning, "Table Albums cleared succesfuly");

      //      foreach (var album in albums)
      //      {
      //        var itemChange = new ItemChanged()
      //        {
      //          Changed = Changed.Removed,
      //          Item = album
      //        };

      //        ItemChanged.OnNext(itemChange);
      //      }

      //      ActionIsDone.OnNext(Unit.Default);

      //      var artists = context.Artists.ToList();

      //      await context.Database.ExecuteSqlCommandAsync("DELETE FROM Artists");
      //      logger.Log(Logger.MessageType.Warning, "Table Artists cleared succesfuly");

      //      foreach (var artist in artists)
      //      {
      //        ItemChanged.OnNext(new ItemChanged()
      //        {
      //          Changed = Changed.Removed,
      //          Item = artist
      //        });
      //      }

      //      ActionIsDone.OnNext(Unit.Default);

      //    }
      //    catch (Exception ex)
      //    {
      //      logger.Log(Logger.MessageType.Inform, ex.Message);
      //    }
      //  }
      //});
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
            UpdateEntityAsync(item);
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
               select x).Include(x => x.Songs)
              .ThenInclude(x => x.ItemModel)
              .ThenInclude(x => x.FileInfoEntity)
              .Include(x => x.Artist).SingleOrDefault();

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
                album.Songs = null;
                originalAlbum.Update(album);

                var duplicates = (from x in albums
                                  where x.NormalizedName == originalAlbum.NormalizedName
                                  where x.Artist.NormalizedName == originalAlbum.Artist.NormalizedName
                                  group x by x.Name
                  into a
                                  where a.Count() > 1
                                  select a.ToList()).SingleOrDefault();

                if (duplicates == null)
                {

                  context.SaveChanges();
                  logger.Log(Logger.MessageType.Success,
                    $"Album was updated in database {album.Name}");

                  PublishItemChanged(originalAlbum);
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

                    PublishItemChanged(originalCopy);

                  }
                }
              }
              else
              {
                originalAlbum =
                  (from x in context.Albums
                   where string.IsNullOrEmpty(x.NormalizedName)
                   where string.IsNullOrEmpty(x.Artist.NormalizedName)
                   where x.NormalizedName == album.NormalizedName
                   where x.Artist.NormalizedName == album.Artist.NormalizedName
                   select x).Include(x => x.Songs).ThenInclude(x => x.ItemModel).ThenInclude(x => x.FileInfoEntity)
                  .Include(x => x.Artist).SingleOrDefault();

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

                PublishItemChanged(originalAlbum.Artist);

              }
            }
            else
            {
              if (album.Songs == null || album.Songs.Count == 0)
              {
                var dbAlbum = context.Albums.Where(x => x.Id == album.Id)
                  .Include(x => x.Songs)
                  .ThenInclude(x => x.ItemModel)
                  .ThenInclude(x => x.FileInfoEntity)
                  .Include(x => x.Artist).SingleOrDefault();


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

    #region UpdateSong

    public Task<bool> UpdateSong(Song newVersion, bool updateAlbum = false, Album album = null)
    {
      return Task.Run(() =>
      {
        try
        {
          bool result = false;

          using (var context = new AudioDatabaseContext())
          {
            var foundEntity = GetRepository<Song>(context).Include(x => x.Album.Songs).SingleOrDefault(x => x.Id == newVersion.Id);

            if (foundEntity != null)
            {
              if (updateAlbum)
              {
                if (foundEntity.Album?.Songs != null && (foundEntity.Album.Songs?.Count == 0 || foundEntity.Album.Songs[0].Id == foundEntity.Id) && album == null)
                {
                  context.Remove(foundEntity.Album);
                  context.Entry(foundEntity.Album).State = EntityState.Deleted;
                }

                foundEntity.Album = album;
              }

              foundEntity.Update(newVersion);
              context.Entry(newVersion).State = EntityState.Modified;

              var updateCount = context.SaveChanges();
              result = updateCount > 0;

              logger.Log(Logger.MessageType.Success, $"Song was updated {newVersion} update count {updateCount}");

              PublishItemChanged(foundEntity);

              return result;
            }

            return result;
          }
        }
        catch (Exception ex)
        {
          logger.Log(ex);

          return false;
        }
      });
    }

    #endregion

    #region ResetSongs

    public Task<bool> ResetSongs(IEnumerable<Song> newVersions)
    {
      return Task.Run(() =>
      {
        try
        {
          bool result = false;
          var list = newVersions.ToList();

          using (var context = new AudioDatabaseContext())
          {
            var ids = list.Select(x => x.Id);

            var foundEntities = GetRepository<Song>(context).Include(x => x.Album).ThenInclude(x => x.Songs).Where(x => ids.Contains(x.Id));

            foreach (var foundEntity in foundEntities)
            {
              if (foundEntity.Album?.Songs != null && (foundEntity.Album.Songs?.Count == 0 || foundEntity.Album.Songs[0].Id == foundEntity.Id))
              {
                var album = foundEntity.Album;
                var dbArtist = GetRepository<Artist>(context).Include(x => x.Albums).SingleOrDefault(x => x.Albums.Any(album => album.Id == foundEntity.Album.Id));

                if (dbArtist?.Albums != null && (dbArtist.Albums.Count == 0 || (dbArtist.Albums.First().Id == foundEntity.Album.Id && dbArtist.Albums.Count == 1)))
                {
                  var artist = dbArtist;

                  context.Entry(album).State = EntityState.Deleted;
                  context.Entry(artist).State = EntityState.Deleted;

                  PublishItemChanged(album, Changed.Removed);
                  PublishItemChanged(artist, Changed.Removed);
                }
                else if (album?.Songs != null && album.Songs.Count == 0 || album.Songs.Count == 1)
                {
                  album.Artist?.Albums?.Remove(album);
                  context.Entry(album).State = EntityState.Deleted;
                  PublishItemChanged(album, Changed.Removed);
                }

                album.Songs.Remove(foundEntity);
                context.Entry(foundEntity).State = EntityState.Deleted;
                PublishItemChanged(foundEntity, Changed.Removed);
              }
            }

            var updateCount = context.SaveChanges();

            result = updateCount > 0;
            logger.Log(Logger.MessageType.Success, $"Songs was updated update count {updateCount}");

            if (result)
            {
              foreach (var entity in list)
              {
                PublishItemChanged(entity);
              }
            }

            return result;
          }
        }
        catch (Exception ex)
        {
          logger.Log(ex);

          return false;
        }
      });
    }

    #endregion

    #region CombineAlbums

    private Album CombineAlbums(Album originalAlbum, Album albumToCombine, AudioDatabaseContext context)
    {
      if (originalAlbum.Id != albumToCombine.Id)
      {
        try
        {
          var songsToAdd = (from x in albumToCombine.Songs
                            where originalAlbum.Songs.All(y => y.ItemModel.NormalizedName != x.ItemModel.NormalizedName)
                            select x)
            .ToList();

          originalAlbum.Songs.AddRange(songsToAdd);

          context.Albums.Remove(albumToCombine);
          context.SaveChanges();

          PublishItemChanged(albumToCombine, Changed.Removed);
          PublishItemChanged(originalAlbum);

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

      return originalAlbum;
    }

    #endregion CombineAlbums

    #region Generic methods

    #region StoreEntity

    public bool StoreEntity<TEntity>(TEntity entity, out TEntity entityModel, bool log = true) where TEntity : class, IEntity
    {
      using (var context = new AudioDatabaseContext())
      {
        TEntity foundEntity = default(TEntity);

        if (entity.Id > 0)
          foundEntity = GetRepository<TEntity>(context).SingleOrDefault(x => x.Id == entity.Id);

        if (foundEntity == null)
        {
          context.Add(entity);

          var result = context.SaveChanges() > 0;

          entityModel = entity;

          if (result)
          {
            if (log)
              logger.Log(Logger.MessageType.Success, $"Entity was stored {entity}");

            PublishItemChanged(entity, Changed.Added);

          }

          return result;
        }

        entityModel = null;
        return false;
      }
    }

    #endregion

    #region StoreAlbum

    public bool StoreAlbum(Album entity, out Album entityModel, bool log = true)
    {
      using (var context = new AudioDatabaseContext())
      {
        var foundEntity = GetRepository<Album>(context).SingleOrDefault(x => x.Id == entity.Id);

        if (foundEntity == null)
        {
          var newSongs = entity.Songs.Where(x => x.Id == 0);
          var existingSongs = entity.Songs.Where(x => x.Id != 0).ToList();

          entity.Songs = new List<Song>(newSongs);

          context.Add(entity);

          var result = context.SaveChanges() > 0;

          if (result)
          {
            if (existingSongs.Any())
            {
              entity.Songs.AddRange(existingSongs);
              context.Update(entity);

              var update = context.SaveChanges();
            }

            if (log)
              logger.Log(Logger.MessageType.Success, $"Entity was stored {entity}");

            PublishItemChanged(entity, Changed.Added);

          }

          entityModel = entity;

          return result;
        }

        entityModel = null;
        return false;
      }
    }

    #endregion

    #region StoreRangeEntity

    public bool StoreRangeEntity<TEntity>(List<TEntity> entities, bool log = true) where TEntity : class, IEntity
    {
      using (var context = new AudioDatabaseContext())
      {
        context.AddRange(entities);

        var count = context.SaveChanges();
        var result = count > 0;

        if (result)
        {
          if (log)
            logger.Log(Logger.MessageType.Success, $"Entities was stored {count}");

          foreach (var entity in entities)
          {
            PublishItemChanged(entity, Changed.Added);
          }
        }

        return result;
      }
    }

    #endregion

    #region UpdateEntity

    public Task<bool> UpdateEntityAsync<TEntity>(TEntity newVersion) where TEntity : class, IEntity, IUpdateable<TEntity>
    {
      Task.Run(() =>
      {
        try
        {
          bool result = false;

          using (var context = new AudioDatabaseContext())
          {
            var foundEntity = GetRepository<TEntity>(context).SingleOrDefault(x => x.Id == newVersion.Id);

            if (foundEntity != null)
            {
              if (!EqualityComparer<TEntity>.Default.Equals(foundEntity, newVersion))
              {
                foundEntity.Update(newVersion);

                var updateCount = context.SaveChanges();
                result = updateCount > 0;

                logger.Log(Logger.MessageType.Success, $"Entity was updated {newVersion} update count {updateCount}");

                PublishItemChanged(foundEntity);
              }

            }
          }

          return result;
        }
        catch (Exception ex)
        {
          logger.Log(ex);

          return false;
        }
      });

      return Task.FromResult(true);
    }

    #endregion

    #region UpdateEntities

    public Task<bool> UpdateEntitiesAsync<TEntity>(IEnumerable<TEntity> newVersions) where TEntity : class, IEntity, IUpdateable<TEntity>
    {
      return Task.Run(() =>
      {
        try
        {
          bool result = false;
          var newVersionsList = newVersions.ToList();

          using (var context = new AudioDatabaseContext())
          {
            var foundEntities = GetRepository<TEntity>(context).Where(dbEntity => newVersionsList.Select(x => x.Id).Contains(dbEntity.Id));

            foreach (var dbEntity in foundEntities)
            {
              dbEntity.Update(newVersionsList.First(x => x.Id == dbEntity.Id));
            }
            var updateCount = context.SaveChanges();

            result = updateCount > 0;

            logger.Log(MessageType.Success, $"Entities was updated update count {updateCount}");

            foreach (var dbEntity in foundEntities)
            {
              PublishItemChanged(dbEntity);
            }

            return result;
          }
        }
        catch (Exception ex)
        {
          logger.Log(ex);

          return false;
        }
      });
    }

    #endregion

    #region DeleteEntity

    public bool DeleteEntity<TEntity>(TEntity entity) where TEntity : class, IEntity
    {
      if (entity is TvShow tvShow)
      {
        return DeleteTvShow(tvShow);
      }
      else if (entity is Album album)
      {
        return DeleteAlbum(album);
      }
      else if (entity is Artist artist)
      {
        return DeleteArtist(artist);
      }
      else
      {
        using (var context = new AudioDatabaseContext())
        {
          var foundEntity = GetRepository<TEntity>(context).SingleOrDefault(x => x.Id == entity.Id);
          bool result = false;

          if (foundEntity != null)
          {
            context.Remove(foundEntity);

            var removedResult = context.SaveChanges();

            result = removedResult > 0;

            logger.Log(Logger.MessageType.Success, $"Entity was removed {foundEntity} {removedResult}");

            PublishItemChanged(foundEntity, Changed.Removed);
          }

          return result;
        }
      }
    }

    #endregion

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

          PublishItemChanged(foundEntity);

        }
      }
    }

    #endregion

    #region DeleteAlbum

    public bool DeleteAlbum(Album album, AudioDatabaseContext context = null)
    {
      bool saveResult = context == null;

      if (context == null)
      {
        context = new AudioDatabaseContext();
      }

      var tvShowRepo = GetRepository<Album>(context);

      var foundEntity = tvShowRepo.Include(x => x.Songs)
        .ThenInclude(x => x.ItemModel)
        .ThenInclude(x => x.FileInfoEntity)
        .SingleOrDefault(x => x.Id == album.Id);

      bool result = false;

      if (foundEntity != null)
      {
        foreach (var song in foundEntity.Songs)
        {
          //if (song.ItemModel != null)
          //{
          //  if (song.ItemModel.FileInfo != null)
          //  {
          //    context.Remove(song.ItemModel?.FileInfo);
          //  }

          //  context.Remove(song.ItemModel);
          //}

          context.Remove(song);
        }

        context.Remove(foundEntity);

        if (saveResult)
        {
          result = context.SaveChanges() > 0;

          if (result)
          {
            logger.Log(Logger.MessageType.Success, $"Entity ALBUM was deleted {album.Name}");

            PublishItemChanged(foundEntity, Changed.Removed);


          }
        }
        else
        {
          result = true;
        }
      }

      if (saveResult)
      {
        context.Dispose();
      }

      return result;
    }

    #endregion

    #region DeleteArtist

    public bool DeleteArtist(Artist artist)
    {
      using (var context = new AudioDatabaseContext())
      {
        var foundEntity = GetRepository<Artist>(context)
          .Include(x => x.Albums)
          .SingleOrDefault(x => x.Id == artist.Id);

        bool result = false;

        if (foundEntity != null)
        {
          if (foundEntity.Albums != null)
          {
            foreach (var album in foundEntity.Albums)
            {
              DeleteAlbum(album, context);
            }
          }

          context.Remove(foundEntity);

          var removedResult = context.SaveChanges();

          result = removedResult > 0;

          logger.Log(Logger.MessageType.Success, $"Entity was removed {foundEntity} {removedResult}");


          PublishItemChanged(foundEntity, Changed.Removed);

        }

        return result;
      }
    }

    #endregion

    #region Playlist methods

    #region DeletePlaylist

    public Task DeletePlaylist<TPlaylist, TPlaylistItem>(TPlaylist songsPlaylist)
      where TPlaylist : class, IPlaylist<TPlaylistItem>
      where TPlaylistItem : class
    {
      return Task.Run(() =>
      {
        try
        {
          using (var context = new AudioDatabaseContext())
          {
            var playlistRepo = GetRepository<TPlaylist>(context);
            var itemsRepo = GetRepository<TPlaylistItem>(context);

            var entityPlaylist = playlistRepo.Include(x => x.PlaylistItems).SingleOrDefault(x => x.Id == songsPlaylist.Id);

            if (entityPlaylist != null)
            {
              entityPlaylist.ActualItemId = null;
              entityPlaylist.ActualItem = null;

              context.SaveChanges();

              foreach (var songInPlaylist in entityPlaylist.PlaylistItems)
              {
                itemsRepo.Remove(songInPlaylist);
              }

              playlistRepo.Remove(entityPlaylist);

              var results = context.SaveChanges();

              var result = results > 0;

              if (result)
                logger.Log(MessageType.Success, $"Item was deleted {songsPlaylist} {songsPlaylist.Id} result count {results}");
              else
                logger.Log(MessageType.Warning, $"Item was not deleted {songsPlaylist} {songsPlaylist.Id} result count {results}");


              PublishItemChanged(songsPlaylist, Changed.Removed);

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


    #region UpdatePlaylist

    public bool UpdatePlaylist<TPlaylist, TPlaylistItem, TModel>(TPlaylist playlist, out TPlaylist updatedPlaylist)
      where TPlaylist : class, IPlaylist<TPlaylistItem>
      where TPlaylistItem : class, IItemInPlaylist<TModel>
      where TModel : IEntity
    {
      using (var context = new AudioDatabaseContext())
      {
        var result = false;
        updatedPlaylist = null;
        IPlaylist<TPlaylistItem> foundPlaylist = null;

        if (playlist is SoundItemFilePlaylist)
        {
          foundPlaylist = (IPlaylist<TPlaylistItem>)GetTempRepository<SoundItemFilePlaylist>()
            .Include(x => x.PlaylistItems)
            .ThenInclude(x => x.ReferencedItem.FileInfoEntity)
            .Include(x => x.ActualItem.ReferencedItem)
            .SingleOrDefault(x => x.Id == playlist.Id);
        }
        else
        {
          foundPlaylist = GetTempRepository<TPlaylist>()
            .Include(x => x.PlaylistItems)
            .ThenInclude(x => x.ReferencedItem)
            .Include(x => x.ActualItem.ReferencedItem)
            .SingleOrDefault(x => x.Id == playlist.Id);
        }


        if (foundPlaylist != null)
        {
          context.Entry(foundPlaylist).State = EntityState.Modified;
          var oldHash = playlist.HashCode;
          var oldItems = playlist.PlaylistItems;

          foundPlaylist.Update(playlist);

          if (foundPlaylist.PlaylistItems != null && (oldHash != foundPlaylist.HashCode || oldItems.Count != foundPlaylist.PlaylistItems.Count))
          {
            if (oldItems.Count > 0)
            {
              var removedItems = foundPlaylist.PlaylistItems.Where(p => oldItems.All(p2 => p2.Id != p.Id)).ToList();

              foreach (var removed in removedItems)
              {
                if (foundPlaylist.ActualItemId == removed.Id)
                {
                  foundPlaylist.ActualItemId = null;
                }

                context.Entry(removed).State = EntityState.Deleted;
              }

              var items = foundPlaylist.PlaylistItems.ToList();
              foundPlaylist.PlaylistItems.Clear();

              foreach (var playlistItem in oldItems)
              {
                foundPlaylist.PlaylistItems.Add(playlistItem);

                if (playlistItem.Id == 0)
                {
                  context.Entry(playlistItem).State = EntityState.Added;
                }
                else
                {
                  var existing = items.SingleOrDefault(x => x.Id == playlistItem.Id);

                  if (existing != null)
                  {
                    if (existing.Compare(playlistItem))
                    {
                      context.Entry(playlistItem).State = EntityState.Modified;
                    }
                  }
                }
              }

              foundPlaylist.ItemCount = foundPlaylist.PlaylistItems.Count;
            }
          }

          var actualItem = foundPlaylist.ActualItem;
          foundPlaylist.ActualItem = null;

          var resultCount = context.SaveChanges();

          if (actualItem != null)
          {
            foundPlaylist.ActualItem = actualItem;
            resultCount = context.SaveChanges();
          }

          result = resultCount > 0;

          if (result)
          {
            if (foundPlaylist.PlaylistItems != null &&
                foundPlaylist.ActualItem != null &&
                foundPlaylist.ActualItem.ReferencedItem == null)
            {
              playlist.ActualItem = foundPlaylist.PlaylistItems.SingleOrDefault(x => x.Id == playlist.ActualItem.Id);
            }

            PublishItemChanged(playlist);
          }

          if (result)
            logger.Log(MessageType.Success, $"Item was updated {playlist} {playlist.Id} result count {resultCount}");
          else
            logger.Log(MessageType.Warning, $"Item was not updated {playlist} {playlist.Id} result count {resultCount}");

          updatedPlaylist = (TPlaylist)foundPlaylist;
        }

        return result;
      }
    }

    #endregion

    #endregion

    #endregion

    #region TvShow methods

    #region UpdateWholeTvShow

    private static object updatedBatton = new object();
    public Task<bool> DeepUpdateTvShow(TvShow newVersion)
    {
      return Task.Run(() =>
      {

        lock (updatedBatton)
        {
          using (var context = new AudioDatabaseContext())
          {
            var foundEntity = context.TvShows.AsNoTracking().Include(x => x.Seasons)
              .ThenInclude(x => x.Episodes)
              .ThenInclude(x => x.VideoItem)
              .SingleOrDefault(x => x.Id == newVersion.Id);

            if (foundEntity != null)
            {
              foundEntity.Update(newVersion);

              ModifiedExistingSeasons(context, foundEntity);

              AddNewSeasons(context, foundEntity);

              context.Entry(foundEntity).State = EntityState.Modified;

              var result = context.SaveChanges();

              logger.Log(Logger.MessageType.Success, $"Entity was updated {result}");

              PublishItemChanged(foundEntity);


              return true;
            }

            return false;
          }
        }
      });
    }

    #region ModifiedExistingSeasons

    private void ModifiedExistingSeasons(AudioDatabaseContext context, TvShow foundEntity)
    {
      var existingSeasons = foundEntity.Seasons.Where(x => x.Id != 0).ToList();

      foreach (var season in existingSeasons)
      {
        context.Entry(season).State = EntityState.Modified;
      }

      var existingEpisodes = existingSeasons.SelectMany(x => x.Episodes).Where(x => x.Id != 0);

      foreach (var episode in existingEpisodes)
      {
        context.Entry(episode).State = EntityState.Modified;
        context.Entry(episode.VideoItem).State = EntityState.Modified;
      }
    }

    #endregion

    #region AddNewSeasons

    private void AddNewSeasons(AudioDatabaseContext context, TvShow foundEntity)
    {
      var newSeasons = foundEntity.Seasons.Where(x => x.Id == 0).ToList();

      foreach (var season in newSeasons)
      {
        context.Entry(season).State = EntityState.Added;
      }

      var newEpisodes = newSeasons.SelectMany(x => x.Episodes).Where(x => x.Id == 0);

      foreach (var episode in newEpisodes)
      {
        context.Entry(episode).State = EntityState.Added;
        context.Entry(episode.VideoItem).State = EntityState.Added;
      }
    }

    #endregion

    #endregion

    #region DeleteTvShow

    public bool DeleteTvShow(TvShow tvShow)
    {
      using (var context = new AudioDatabaseContext())
      {
        var tvShowRepo = GetRepository<TvShow>(context);

        var foundEntity = tvShowRepo.Include(x => x.Seasons).ThenInclude(x => x.Episodes).SingleOrDefault(x => x.Id == tvShow.Id);
        bool result = false;

        if (foundEntity != null)
        {
          foreach (var tvShowSeason in foundEntity.Seasons)
          {
            foreach (var tvShowEpisode in tvShowSeason.Episodes)
            {
              context.Remove(tvShowEpisode);
            }

            context.Remove(tvShowSeason);
          }

          context.Remove(foundEntity);

          result = context.SaveChanges() > 0;

          if (result)
          {
            logger.Log(Logger.MessageType.Success, $"Entity TVSHOW was deleted {tvShow.Name}");

            PublishItemChanged(foundEntity, Changed.Removed);

          }
        }

        return result;
      }
    }

    #endregion

    #endregion

    #region Tv methods

    public Task<bool> DeleteTvChannelGroup(TvChannelGroup tvChannelGroup)
    {
      return Task.Run(() =>
      {
        using (var context = new AudioDatabaseContext())
        {
          foreach (var item in tvChannelGroup.TvChannelGroupItems)
          {
            item.TvChannel = null;
            context.TvChannelGroupItems.Remove(item);
          }

          context.TvChannelGroups.Remove(tvChannelGroup);

          var resultCount = context.SaveChanges();
          var result = resultCount > 0;

          if (result)
          {
            logger.Log(Logger.MessageType.Success, $"Entity was removed {tvChannelGroup} {resultCount}");

            PublishItemChanged(tvChannelGroup, Changed.Removed);
          }
          else
            logger.Log(Logger.MessageType.Error, $"Entity was not removed {tvChannelGroup} {resultCount}");


          return result;
        }
      });
    }



    #endregion

    #region PublishItemChanged

    public void PublishItemChanged<TModel>(TModel model, Changed changed = Changed.Updated)
    {
      var newEvent = new ItemChanged<TModel>(model)
      {
        Changed = changed,
      };

      if (!itemChanged.IsDisposed)
        itemChanged.OnNext(newEvent);
    }

    #endregion

    #region DownloadAllNotYetDownloaded

    public Task DownloadAllNotYetDownloaded(bool tryDownloadBroken = false)
    {
      return Task.Run(async () =>
      {
        using (var context = new AudioDatabaseContext())
        {
          var repository = GetRepository<Album>(context);

          var notUpdatedAlbums = repository
            .Where(x => x.InfoDownloadStatus == InfoDownloadStatus.Waiting || x.InfoDownloadStatus == InfoDownloadStatus.Downloading)
            .Include(x => x.Artist).OrderByDescending(x => x.InfoDownloadStatus).ToList();

          foreach (var album in notUpdatedAlbums)
          {
            await audioInfoDownloader.UpdateItem(album);
          }

          if (tryDownloadBroken)
          {
            var brokenAlbums = repository.Where(x => x.InfoDownloadStatus == InfoDownloadStatus.Failed
                                                     || x.InfoDownloadStatus == InfoDownloadStatus.UnableToFind).Include(x => x.Artist);

            foreach (var album in brokenAlbums)
            {
              await audioInfoDownloader.UpdateItem(album);
            }
          }

          context.SaveChanges();
        }
      });
    }

    #endregion

    #region SubscribeToItemChange

    public IDisposable SubscribeToItemChange<TModel>(Action<IItemChanged<TModel>> observer)
    {
      return itemChanged.OfType<IItemChanged<TModel>>().Subscribe(observer);
    }

    #endregion

    #region ObserveOnItemChange

    public IObservable<IItemChanged<TModel>> ObserveOnItemChange<TModel>()
    {
      return itemChanged.OfType<IItemChanged<TModel>>();
    }

    #endregion

    #region StoreTvShow

    public Task<int> StoreTvShow(TvShow tvShow)
    {
      return Task.Run(() =>
      {
        using (var context = new AudioDatabaseContext())
        {
          var repository = GetRepository<TvShow>(context);

          repository.Add(tvShow);

          context.SaveChanges();

          PublishItemChanged(tvShow, Changed.Added);

          return tvShow.Id;
        }
      });
    }

    #endregion

    #region AddPinnedItem

    public Task<PinnedItem> AddPinnedItem(PinnedItem pinnedItem)
    {
      return Task.Run(() =>
      {
        using (var context = new AudioDatabaseContext())
        {
          var repository = GetRepository<PinnedItem>(context);

          repository.Add(pinnedItem);

          context.SaveChanges();

          PublishItemChanged(pinnedItem, Changed.Added);

          return pinnedItem;
        }
      });
    }

    #endregion

    #region RemovePinnedItem

    public Task<bool> RemovePinnedItem(PinnedItem pinnedItem)
    {
      return Task.Run(() =>
      {
        using (var context = new AudioDatabaseContext())
        {
          var repository = GetRepository<PinnedItem>(context);

          repository.Remove(pinnedItem);

          var result = context.SaveChanges() > 0;

          PublishItemChanged(pinnedItem, Changed.Removed);

          return result;
        }
      });
    }

    #endregion

    #region Dispose

    public void Dispose()
    {
      itemChanged?.Dispose();
      ActionIsDone?.Dispose();
      disposable?.Dispose();
    }

    #endregion


    #endregion

  }
}