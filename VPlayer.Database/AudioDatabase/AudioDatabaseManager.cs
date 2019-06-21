using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            get { return AudioDatabaseContext.Albums; }
        }

        public AudioDatabaseManager()
        {
            AudioDatabaseContext = new AudioDatabaseContext();
        }
        public async Task StoreData(AudioInfo audioInfo)
        {
            using (var context = new AudioDatabaseContext())
            {
                try
                {
                    Logger.Logger.Instance.Log(typeof(AudioDatabaseManager), $"Storing audio {audioInfo.DiskLocation}");

                    Artist artist = new Artist(audioInfo.Artist)
                    {
                        MusicBrainzId = audioInfo.ArtistMbid
                    };

                    artist = (from x in context.Artists where x.Hash == artist.Hash select x).SingleOrDefault();

                    if (artist == null)
                    {
                        artist = new Artist(audioInfo.Artist)
                        {
                            MusicBrainzId = audioInfo.ArtistMbid
                        };

                        context.Artists.Add(artist);
                        await context.SaveChangesAsync();

                        Logger.Logger.Instance.Log(typeof(AudioDatabaseManager), $"New artist was added {artist.Name}");
                    }

                    Album album = new Album(audioInfo.Album, artist);

                    album = (from x in context.Albums where x.Hash == album.Hash select x).SingleOrDefault();

                    if (album == null)
                    {
                        album = new Album(audioInfo.Album, artist);

                        context.Albums.Add(album);
                        context.SaveChanges();

                        //Need albumId from database
                        album = (from x in context.Albums where x.Hash == album.Hash select x).Single();

                        Logger.Logger.Instance.Log(typeof(AudioDatabaseManager), $"New album was added {album.Name}");

                        StorageManager.OnAlbumStored(album);
                    }

                    Song song = new Song(audioInfo.Title, album, artist)
                    {
                        DiskLocation = audioInfo.DiskLocation
                    };

                    song = (from x in context.Songs where song.Hash == x.Hash select x).SingleOrDefault();

                    if (song == null)
                    {
                        song = new Song(audioInfo.Title, album, artist)
                        {
                            DiskLocation = audioInfo.DiskLocation
                        };

                        context.Songs.Add(song);
                        await context.SaveChangesAsync();

                        Logger.Logger.Instance.Log(typeof(AudioDatabaseManager),
                            $"Audio stored {audioInfo.DiskLocation}");
                    }
                    else
                        Logger.Logger.Instance.Log(typeof(AudioDatabaseManager),
                            $"Audio was already in database {audioInfo.DiskLocation}");


                    //_audioDatabase.Genres.Add(genre);

                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        if (ex.InnerException.InnerException != null)
                        {
                            Logger.Logger.Instance.Log(typeof(AudioDatabaseManager),
                                $"{ex.InnerException.InnerException.Message}");
                        }
                        else
                            Logger.Logger.Instance.Log(typeof(AudioDatabaseManager), $"{ex.InnerException.Message}");

                    }
                    else
                    {
                        Logger.Logger.Instance.Log(typeof(AudioDatabaseManager), $"{ex.Message}");

                    }
                }
            }
        }

        public async Task StoreData(List<AudioInfo> audioInfos)
        {
            foreach (var audioInfo in audioInfos)
            {
                await StoreData(audioInfo);
            }
        }

        public async Task ClearStorage()
        {
            using (var context = new AudioDatabaseContext())
            {
                try
                {
                    await Task.Run(() =>
                    {
                        context.Database.ExecuteSqlCommand("DELETE FROM Songs");
                        Logger.Logger.Instance.Log(typeof(AudioDatabaseManager), "Table Songs cleared succesfuly");

                        context.Database.ExecuteSqlCommand("DELETE FROM Genres");
                        Logger.Logger.Instance.Log(typeof(AudioDatabaseManager), "Table Genres cleared succesfuly");

                        context.Database.ExecuteSqlCommand("DELETE FROM Albums");
                        Logger.Logger.Instance.Log(typeof(AudioDatabaseManager), "Table Albums cleared succesfuly");

                        context.Database.ExecuteSqlCommand("DELETE FROM Artists");
                        Logger.Logger.Instance.Log(typeof(AudioDatabaseManager), "Table Artists cleared succesfuly");
                    });
                }
                catch (Exception ex)
                {
                    Logger.Logger.Instance.Log(typeof(AudioDatabaseManager), ex.Message);
                }
            }
        }

        public async Task UpdateAlbum(Album album)
        {
            using (var context = new AudioDatabaseContext())
            {
                try
                {
                    Logger.Logger.Instance.Log(typeof(AudioDatabaseManager), $"Updating album {album.Name}");

                    var original = (from x in context.Albums where x.AlbumId == album.AlbumId select x).Single();

                    original.UpdateAlbum(album);

                    await context.SaveChangesAsync();
                    Logger.Logger.Instance.Log(typeof(AudioDatabaseManager), $"Album updated {album.Name}");

                    StorageManager.OnAlbumUpdated(original);
                }
                catch (Exception ex)
                {
                    Logger.Logger.Instance.Log(typeof(AudioDatabaseManager),ex.Message);
                }
            }
        }


        public void Dispose()
        {
            AudioDatabaseContext.SaveChanges();
            AudioDatabaseContext?.Dispose();
        }

    }
}
