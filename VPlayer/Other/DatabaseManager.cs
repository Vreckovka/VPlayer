using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using VPlayer.LocalMusicDatabase;
using VPlayer.Other.AudioInfoDownloader;
using Artist = Hqub.MusicBrainz.API.Entities.Artist;

namespace VPlayer.Other
{
    static class DatabaseManager
    {
        private static LocalMusicDbContext _localMusicDbContext;
        static DatabaseManager()
        {
            _localMusicDbContext = new LocalMusicDbContext();
        }


        public static async Task UpdateDiscLocationOfSong(AudioInfo songInfo, string songLocation)
        {
            Song songDb = null;

            if (songInfo.ArtistMBID != null)
                songDb = (from x in _localMusicDbContext.Songs
                          join y in _localMusicDbContext.Albums on x.Album equals y
                          join z in _localMusicDbContext.Artists on y.Artist equals z
                          where x.Name == songInfo.Title
                          where z.MusicBrainzId == songInfo.ArtistMBID
                          select x).FirstOrDefault();
            else
                songDb = (from x in _localMusicDbContext.Songs
                          join y in _localMusicDbContext.Albums on x.Album equals y
                          join z in _localMusicDbContext.Artists on y.Artist equals z
                          where x.Name == songInfo.Title
                          where z.Name == songInfo.Artist
                          select x).FirstOrDefault();


            if (songDb != null)
            {
                songDb.DiskLocation = songLocation;
                await _localMusicDbContext.SaveChangesAsync();

                Console.WriteLine(songDb.Name + " was updated in db");
            }
            else
            {
                AudioInfo audioInfo = await AudioInfoDownloader.AudioInfoDownloader.GetTrackInfoByFingerPrint(songLocation, songInfo);

                songDb = (from x in _localMusicDbContext.Songs
                          join y in _localMusicDbContext.Albums on x.Album equals y
                          join z in _localMusicDbContext.Artists on y.Artist equals z
                          where x.Name == audioInfo.Title
                          where z.MusicBrainzId == audioInfo.ArtistMBID
                          select x).FirstOrDefault();

                if (songDb != null)
                {
                    songDb.DiskLocation = songLocation;
                    await _localMusicDbContext.SaveChangesAsync();

                    Console.WriteLine(songDb.Name + " was updated in db");
                }
            }
        }
        public static bool IsArtistInTheDatabase(JObject artist)
        {
            dynamic jObject = artist;

            LocalMusicDatabase.Artist artistDb = new LocalMusicDatabase.Artist()
            {
                Name = jObject.name,
                MusicBrainzId = jObject.id,
            };

            var query = (from x in _localMusicDbContext.Artists where x.MusicBrainzId == artistDb.MusicBrainzId select x).Count();

            if (query == 0)
            {
                return false;
            }

            return true;
        }

        public static async Task AddAllInfoOfArtistToDatabase(JObject artist)
        {
            dynamic jObject = artist;

            LocalMusicDatabase.Artist artistDb = new LocalMusicDatabase.Artist()
            {
                Name = jObject.name,
                MusicBrainzId = jObject.id,
            };


            var albums = AudioInfoDownloader.AudioInfoDownloader.GetArtistsAlbums(artistDb);
            artistDb.Albums = await albums;

            _localMusicDbContext.Artists.Add(artistDb);
            _localMusicDbContext.SaveChanges();

        }
    }
}
