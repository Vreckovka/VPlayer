using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using VCore.Modularity.Events;
using VPlayer.AudioStorage.Interfaces;
using VPlayer.Core.DomainClasses;

namespace VPlayer.AudioStorage.AudioDatabase
{
  public interface IGenericRepository<T> where T : class
  {
    #region Methods

    void Add(T entity);

    void Delete(T entity);

    void Edit(T entity);

    IQueryable<T> FindBy(Expression<Func<T, bool>> predicate);

    IQueryable<T> GetAll();

    void Save();

    #endregion Methods
  }

  public class ArtistRepository : GenericRepository<AudioDatabaseContext, Artist>
  {
  }

  public class AudioDatabaseManager : IStorageManager
  {
    #region Fields

    private readonly VPlayer.AudioInfoDownloader.AudioInfoDownloader audioInfoDownloader;

    #endregion Fields

    #region Constructors

    public AudioDatabaseManager(VPlayer.AudioInfoDownloader.AudioInfoDownloader audioInfoDownloader)
    {
      this.audioInfoDownloader = audioInfoDownloader ?? throw new ArgumentNullException(nameof(audioInfoDownloader));

      audioInfoDownloader.ItemUpdated.Subscribe(ItemUpdated);
    }

    #endregion Constructors

    #region Properties

    public Subject<ItemChanged> ItemChanged { get; } = new Subject<ItemChanged>();

    #endregion Properties

    #region GetRepository

    public IQueryable<T> GetRepository<T>(DbContext dbContext = null) where T : class
    {
      if (dbContext == null)
        dbContext = new AudioDatabaseContext();

      IQueryable<T> query = dbContext.Set<T>();

      return query;
    }

    #endregion GetRepository



    #region StoreData

    public async Task StoreData(AudioInfo audioInfo)
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

          if (artist == null)
          {
            artist = new Artist(audioInfo.Artist)
            {
              MusicBrainzId = audioInfo.ArtistMbid
            };

            context.Artists.Add(artist);
            await context.SaveChangesAsync();

            Logger.Logger.Instance.Log(Logger.MessageType.Success, $"New artist was added {artist.Name}");

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
            Artist = artist
          };

          album = (from x in context.Albums
                   where x.Name == album.Name
                   where x.Artist.Name == album.Artist.Name
                   select x).SingleOrDefault();

          if (album == null)
          {
            album = new Album()
            {
              Name = audioInfo.Album,
              Artist = artist
            };

            context.Albums.Add(album);
            await context.SaveChangesAsync();

            //Need albumId from database
            album = (from x in context.Albums
                     where x.Name == album.Name
                     where x.Artist.Name == album.Artist.Name
                     select x).Single();

            Logger.Logger.Instance.Log(Logger.MessageType.Success, $"New album was added {album.Name}");

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

            context.Songs.Add(song);
            await context.SaveChangesAsync();

            ItemChanged.OnNext(new ItemChanged()
            {
              Item = song,
              Changed = Changed.Added
            });

            Logger.Logger.Instance.Log(Logger.MessageType.Success, $"New song was added {song.Name}");
          }
        }
      }
      catch (Exception ex)
      {
        if (ex.InnerException != null)
        {
          if (ex.InnerException.InnerException != null)
          {
            Logger.Logger.Instance.Log(Logger.MessageType.Error,
                $"{ex.InnerException.InnerException.Message}");
          }
          else
            Logger.Logger.Instance.Log(Logger.MessageType.Error, $"{ex.InnerException.Message}");
        }
        else
        {
          Logger.Logger.Instance.Log(Logger.MessageType.Error, $"{ex.Message}");
        }
      }
    }

    public Task<bool> StoreData(IEnumerable<string> audioPath)
    {
      return Task.Run(async () =>
      {
        bool result = true;

        foreach (var audio in audioPath)
        {
          result = result && await StoreData(audio);
        }

        return result;
      });
    }

    public Task<bool> StoreData(string audioPath)
    {
      return Task.Run(async () =>
      {
        // get the file attributes for file or directory
        FileAttributes attr = File.GetAttributes(audioPath);

        //detect whether its a directory or file
        if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
        {
          var audioInfos = await audioInfoDownloader.GetAudioInfosFromDirectory(audioPath, true);

          await StoreData(audioInfos);
        }
        else
        {
          var audioInfo = await audioInfoDownloader.GetAudioInfo(audioPath);

          if (audioInfo == null)
          {
            return false;
          }

          await StoreData(audioInfo);
        }

        return true;
      });
    }

    public async Task StoreData(List<AudioInfo> audioInfos)
    {
      try
      {
        foreach (var audioInfo in audioInfos)
        {
          if (audioInfo != null)
            await StoreData(audioInfo);
        }
      }
      catch (Exception ex)
      {
        Logger.Logger.Instance.Log(Logger.MessageType.Error, $"{ex.Message}");
      }
    }

    #endregion StoreData

    #region ClearStorage

    public async Task ClearStorage()
    {
      using (var context = new AudioDatabaseContext())
      {
        try
        {
          await context.Database.ExecuteSqlCommandAsync("DELETE FROM Songs");
          Logger.Logger.Instance.Log(Logger.MessageType.Warning, "Table Songs cleared succesfuly");

          //await context.Database.ExecuteSqlCommandAsync("DELETE FROM Genres");
          //Logger.Logger.Instance.Log(Logger.MessageType.Warning, "Table Genres cleared succesfuly");

          foreach (var album in context.Albums)
          {
            ItemChanged.OnNext(new ItemChanged()
            {
              Changed = Changed.Removed,
              Item = album
            });
          }

          await context.Database.ExecuteSqlCommandAsync("DELETE FROM Albums");
          Logger.Logger.Instance.Log(Logger.MessageType.Warning, "Table Albums cleared succesfuly");

          foreach (var artist in context.Artists)
          {
            ItemChanged.OnNext(new ItemChanged()
            {
              Changed = Changed.Removed,
              Item = artist
            });
          }

          await context.Database.ExecuteSqlCommandAsync("DELETE FROM Artists");
          Logger.Logger.Instance.Log(Logger.MessageType.Warning, "Table Artists cleared succesfuly");
        }
        catch (Exception ex)
        {
          Logger.Logger.Instance.Log(Logger.MessageType.Inform, ex.Message);
        }
      }
    }

    #endregion ClearStorage

    #region UpdateItem

    public void ItemUpdated(dynamic item)
    {
      UpdateItem(item);
    }

    public async Task UpdateItem(Artist artist)
    {
      using (var context = new AudioDatabaseContext())
      {
        try
        {
          var originalArtist =
              await (from x in context.Artists where x.Id == artist.Id select x).SingleOrDefaultAsync();

          if (originalArtist != null)
          {
            originalArtist.MusicBrainzId = artist.MusicBrainzId;
            originalArtist.Name = artist.Name;

            ItemChanged.OnNext(new ItemChanged()
            {
              Changed = Changed.Updated,
              Item = originalArtist
            });

            await context.SaveChangesAsync();
          }
        }
        catch (Exception ex)
        {
          Logger.Logger.Instance.LogException(ex);
        }
      }
    }

    #endregion UpdateItem

    #region UpdateAlbum

    public async Task UpdateItem(Album album)
    {
      using (var context = new AudioDatabaseContext())
      {
        try
        {
          var originalAlbum = (from x in context.Albums.Include(x => x.Artist) where x.MusicBrainzId == album.MusicBrainzId select x)
              .SingleOrDefault();

          //Update is first time
          if (originalAlbum == null)
          {
            var albums = context.Albums.ToList().OrderBy(x => x.Name);

            originalAlbum = (from x in context.Albums.Include(x => x.Artist)
                             where x.Id == album.Id
                             select x).SingleOrDefault();

            //Album could be deleted from storage
            if (originalAlbum != null)
            {
              originalAlbum.UpdateAlbum(album);

              var duplicates = (from x in albums
                                where x.Name == originalAlbum.Name
                                where x.Artist.Name == originalAlbum.Artist.Name
                                group x by x.Name into a
                                where a.Count() > 1
                                select a.ToList()).SingleOrDefault();

              if (duplicates == null)
              {
                await context.SaveChangesAsync();
                Logger.Logger.Instance.Log(Logger.MessageType.Success,
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
                   select x).SingleOrDefault();

              if (originalAlbum != null)
                CombineAlbums(originalAlbum, album, context);
              else
                ;
            }
          }
          else
          {
            CombineAlbums(originalAlbum, album, context);
          }
        }
        catch (Exception ex)
        {
          Logger.Logger.Instance.LogException(ex);
        }
      }
    }

    #endregion UpdateAlbum

    #region UpdateAlbums

    public async Task UpdateAlbums(List<Album> albumsToUpdate)
    {
      using (var context = new AudioDatabaseContext())
      {
        foreach (var album in albumsToUpdate)
        {
          try
          {
            var originalAlbum =
                (from x in context.Albums where x.MusicBrainzId == album.MusicBrainzId select x)
                .SingleOrDefault();

            //Update is first time
            if (originalAlbum == null)
            {
              var albums = context.Albums.ToList().OrderBy(x => x.Name);

              originalAlbum = (from x in context.Albums
                               where x.Id == album.Id
                               select x).SingleOrDefault();

              //Album could be deleted from storage
              if (originalAlbum != null)
              {
                originalAlbum.UpdateAlbum(album);

                var duplicates = (from x in albums
                                  where x.Name == originalAlbum.Name
                                  where x.Artist.Name == originalAlbum.Artist.Name
                                  group x by x.Name
                    into a
                                  where a.Count() > 1
                                  select a.ToList()).SingleOrDefault();

                if (duplicates == null)
                {
                  await context.SaveChangesAsync();
                  Logger.Logger.Instance.Log(Logger.MessageType.Success,
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
                     select x).SingleOrDefault();

                if (originalAlbum != null)
                  CombineAlbums(originalAlbum, album, context);
                else
                  ;
              }
            }
            else
            {
              CombineAlbums(originalAlbum, album, context);
            }
          }
          catch (Exception ex)
          {
            Logger.Logger.Instance.LogException(ex);
          }
        }
      }
    }

    #endregion UpdateAlbums

    #region CombineAlbums

    private Album CombineAlbums(Album originalAlbum, Album albumToCombine, AudioDatabaseContext context)
    {
      try
      {
        albumToCombine = (from x in context.Albums where x.Id == albumToCombine.Id select x)
            .SingleOrDefault();

        //Could be disk 1, disk 2 and renamed without disk index
        if (originalAlbum != null && albumToCombine != null)
        {
          if (albumToCombine.Songs.Count == 0)
          {
            Logger.Logger.Instance.Log(Logger.MessageType.Warning,
                $"Album already exists in database, removing album from database {albumToCombine.Name}");
          }
          else
          {
            var songsToAdd =
                (from x in albumToCombine.Songs
                 where originalAlbum.Songs.All(y => y.Name != x.Name)
                 select x)
                .ToList();

            if (songsToAdd.Count == 0)
            {
              Logger.Logger.Instance.Log(Logger.MessageType.Warning,
                  $"Album already exists in database, removing album from database {albumToCombine.Name}");
            }
            else
            {
              originalAlbum.Songs.AddRange(albumToCombine.Songs);
            }
          }

          var originalCopy = originalAlbum;
          var artistCopy = originalAlbum.Artist;

          context.Albums.Remove(originalAlbum);
          context.Albums.Remove(albumToCombine);

          context.SaveChanges();

          originalAlbum.Artist = artistCopy;

          context.Albums.Add(originalCopy);
          context.SaveChanges();

          ItemChanged.OnNext(new ItemChanged()
          {
            Changed = Changed.Removed,
            Item = albumToCombine
          });

          Logger.Logger.Instance.Log(Logger.MessageType.Warning,
              $"Combining album {albumToCombine.Name} to {originalAlbum.Name}");

          return originalCopy;
        }

        return null;
      }
      catch (Exception ex)
      {
        Logger.Logger.Instance.LogException(ex);
        return null;
      }
    }

    #endregion CombineAlbums

    #region UpdateEntity

    public void UpdateEntity<T>(T entity) where T : class, IEntity
    {
      using (var context = new AudioDatabaseContext())
      {
        var foundEntity = GetRepository<T>(context).Single(x => x.Id == entity.Id);

        context.Entry(foundEntity).CurrentValues.SetValues(entity);

        context.SaveChanges();

        Logger.Logger.Instance.Log(Logger.MessageType.Success, $"Album was updated {entity}");

        ItemChanged.OnNext(new ItemChanged()
        {
          Item = foundEntity,
          Changed = Changed.Updated
        });
      }
    }

    #endregion UpdateEntity

    public void Dispose()
    {
    }
  }

  public abstract class GenericRepository<C, T> : IGenericRepository<T> where T : class where C : DbContext, new()
  {
    #region Fields

    private C _entities = new C();

    #endregion Fields

    #region Properties

    public C Context
    {
      get { return _entities; }
      set { _entities = value; }
    }

    #endregion Properties

    #region Methods

    public virtual void Add(T entity)
    {
      _entities.Set<T>().Add(entity);
    }

    public virtual void Delete(T entity)
    {
      _entities.Set<T>().Remove(entity);
    }

    public virtual void Edit(T entity)
    {
      _entities.Entry(entity).State = System.Data.Entity.EntityState.Modified;
    }

    public IQueryable<T> FindBy(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
    {
      IQueryable<T> query = _entities.Set<T>().Where(predicate);
      return query;
    }

    public virtual IQueryable<T> GetAll()
    {
      IQueryable<T> query = _entities.Set<T>();
      return query;
    }

    public virtual void Save()
    {
      _entities.SaveChanges();
    }

    #endregion Methods
  }
}