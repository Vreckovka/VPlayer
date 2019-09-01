using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VPlayer.AudioStorage.Interfaces;
using VPlayer.AudioStorage.Models;

namespace VPlayer.AudioStorage.AudioDatabase
{
    public class AudioDatabaseManager : IStorage
    {
        public AudioDatabaseContext AudioDatabaseContext { get; set; }

        public IEnumerable<Album> Albums
        {
            get
            {
                return AudioDatabaseContext.Albums.Include("Artist").Include("Songs");
            }
        }

        public IEnumerable<Artist> Artists
        {
            get
            {
                return AudioDatabaseContext.Artists.Include("Albums");
            }
        }

        public AudioDatabaseManager()
        {
            AudioDatabaseContext = new AudioDatabaseContext();
        }

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

                        StorageManager.OnArtistStored(artist);
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

                        StorageManager.OnAlbumStored(album);
                    }

                    Song song = new Song(audioInfo.Title, album)
                    {
                        DiskLocation = audioInfo.DiskLocation
                    };

                    song = (from x in context.Songs
                            where song.Name == x.Name
                            where x.Album.AlbumId == song.Album.AlbumId
                            select x).SingleOrDefault();

                    if (song == null)
                    {
                        song = new Song(audioInfo.Title, album)
                        {
                            DiskLocation = audioInfo.DiskLocation
                        };

                        context.Songs.Add(song);
                        await context.SaveChangesAsync();

                        Logger.Logger.Instance.Log(Logger.MessageType.Success, $"New song was added {song.Name}");
                    }

                    //_audioDatabase.Genres.Add(genre);

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

        public async Task ClearStorage()
        {
            using (var context = new AudioDatabaseContext())
            {
                try
                {
                    await context.Database.ExecuteSqlCommandAsync("DELETE FROM Songs");
                    Logger.Logger.Instance.Log(Logger.MessageType.Warning, "Table Songs cleared succesfuly");

                    await context.Database.ExecuteSqlCommandAsync("DELETE FROM Genres");
                    Logger.Logger.Instance.Log(Logger.MessageType.Warning, "Table Genres cleared succesfuly");

                    await context.Database.ExecuteSqlCommandAsync("DELETE FROM Albums");
                    Logger.Logger.Instance.Log(Logger.MessageType.Warning, "Table Albums cleared succesfuly");

                    await context.Database.ExecuteSqlCommandAsync("DELETE FROM Artists");
                    Logger.Logger.Instance.Log(Logger.MessageType.Warning, "Table Artists cleared succesfuly");

                    StorageManager.OnStorageCleared();
                }
                catch (Exception ex)
                {
                    Logger.Logger.Instance.Log(Logger.MessageType.Inform, ex.Message);
                }
            }
        }

        public async Task UpdateArtist(Artist artist)
        {
            try
            {
                var originalArtist = await (from x in AudioDatabaseContext.Artists where x.ArtistId == artist.ArtistId select x).SingleOrDefaultAsync();

                if (originalArtist != null)
                {
                    originalArtist.MusicBrainzId = artist.MusicBrainzId;
                    originalArtist.Name = artist.Name;

                    await AudioDatabaseContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.Instance.LogException(ex);
            }
        }

        public async Task UpdateAlbum(Album album)
        {
            using (var context = new AudioDatabaseContext())
            {
                try
                {
                    var originalAlbum = (from x in context.Albums where x.MusicBrainzId == album.MusicBrainzId select x)
                        .SingleOrDefault();

                    //Update is first time
                    if (originalAlbum == null)
                    {

                        var albums = context.Albums.ToList().OrderBy(x => x.Name);

                        originalAlbum = (from x in context.Albums
                                         where x.AlbumId == album.AlbumId
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

                                StorageManager.OnAlbumUpdated(originalAlbum);
                            }
                            else
                            {
                                Album originalCopy = null;
                                int oldId = duplicates[0].AlbumId;

                                for (int i = 1; i < duplicates.Count; i++)
                                {
                                    originalCopy = CombineAlbums(duplicates[0], duplicates[i], context);
                                }

                                if (originalCopy != null)
                                {
                                    originalCopy.AlbumId = oldId;
                                    StorageManager.OnAlbumUpdated(originalCopy);
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
                                             where x.AlbumId == album.AlbumId
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

                                    StorageManager.OnAlbumUpdated(originalAlbum);
                                }
                                else
                                {
                                    Album originalCopy = null;
                                    int oldId = duplicates[0].AlbumId;

                                    for (int i = 1; i < duplicates.Count; i++)
                                    {
                                        originalCopy = CombineAlbums(duplicates[0], duplicates[i], context);
                                    }

                                    if (originalCopy != null)
                                    {
                                        originalCopy.AlbumId = oldId;
                                        StorageManager.OnAlbumUpdated(originalCopy);
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

        public IEnumerable<TModel> GetItems<TModel>()
        {
            if (typeof(TModel) == typeof(Album))
                return (IEnumerable<TModel>)Albums;
            else if (typeof(TModel) == typeof(Artist))
                return (IEnumerable<TModel>)Artists;

            return null;
        }

        private Album CombineAlbums(Album originalAlbum, Album albumToCombine, AudioDatabaseContext context)
        {
            try
            {
                albumToCombine = (from x in context.Albums where x.AlbumId == albumToCombine.AlbumId select x)
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


                    StorageManager.OnAlbumRemoved(albumToCombine);

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

        public void Dispose()
        {
            AudioDatabaseContext.SaveChanges();
            AudioDatabaseContext?.Dispose();
        }

    }
}
