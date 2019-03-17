using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Windows.Storage;
using Windows.Storage.FileProperties;
using VPlayer.LocalMusicDatabase;
using PropertyChanged;
using VPlayer.Models;
using VPlayer.Other;
using FileAttributes = System.IO.FileAttributes;

namespace VPlayer.ViewModels
{
    public class WindowsPlayerView : BaseViewModel
    {
        public ObservableCollection<AudioTrack> AudioTracks { get; } = new ObservableCollection<AudioTrack>();
        public AudioTrack ActualTrack { get; set; }
        public bool IsPlaying { get; set; }
        public TimeSpan ActualTime { get; set; }
        private DispatcherTimer _actualTimeTimer { get; set; }

        public static event EventHandler<double> ActualTimeChanged;


        public WindowsPlayerView()
        {
            _actualTimeTimer = new DispatcherTimer();
            _actualTimeTimer.Interval = TimeSpan.FromMilliseconds(10);
            _actualTimeTimer.Tick += ActualTime_Tick;

            ActualTimeChanged += AudioTracksView_ActualTimeChanged;

        }

        private void AudioTracksView_ActualTimeChanged(object sender, double e)
        {
            ActualTime = TimeSpan.FromMilliseconds(e);
        }

        private void ActualTime_Tick(object sender, EventArgs e)
        {
            ActualTime = ActualTime.Add(TimeSpan.FromMilliseconds(10));
        }

        public async void GetSongsInfo()
        {

        }

        public void Play(Uri uri = null)
        {
            if (uri == null)
            {
                ActualTrack.IsPlaying = true;
            }
            else
            {
                ActualTime = new TimeSpan();
                ActualTrack.IsPlaying = false;
                ActualTrack = (from x in AudioTracks where x.Uri == uri select x).FirstOrDefault();
                ActualTrack.IsPlaying = true;
            }

            IsPlaying = true;
            PlayerHandler.OnPlay(this);
            _actualTimeTimer.Start();
        }
        public void AddFiles(string[] path)
        {
            foreach (var file in path)
            {
                FileAttributes attr = File.GetAttributes(file);

                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    AddFolder(file);
                else
                    AddFile(file);
            }

            if (ActualTrack == null && AudioTracks.Count > 0)
            {
                ActualTrack = AudioTracks[0];
            }
        }
        public void AddFolder(string folderPath)
        {
            var directories = Directory.GetDirectories(folderPath);

            if (directories.Length == 0)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);
                var Files = directoryInfo.GetFiles("*.mp3");

                foreach (var file in Files)
                {
                    AddFile(file.FullName);
                }
            }
            else
            {
                for (int i = 0; i < directories.Length; i++)
                {
                    AddFolder(directories[i]);
                }
            }
        }

        public void AddFile(string filePath)
        {
            var tagLib = TagLib.File.Create(filePath);

            AudioTrack audioTrack = new AudioTrack()
            {
                Uri = new Uri(filePath),
                Name = tagLib.Tag.Title,
                Duration = tagLib.Properties.Duration,
                Artist = tagLib.Tag.FirstAlbumArtist,
            };

            if (tagLib.Tag.Title == null)
            {
                audioTrack.Name = GetFileName(tagLib);
            }

            Application.Current.Dispatcher.Invoke(() => { AudioTracks.Add(audioTrack); });

        }
        public string GetFileName(TagLib.File file)
        {
            int temp = file.Name.LastIndexOf('\\');
            return file.Name.Substring(temp + 1, file.Name.Length - temp - 1);
        }
        public static void OnActualTimeChanged(double milliseconds)
        {
            ActualTimeChanged?.Invoke(null, milliseconds);
        }

        public async Task UpdateDatabaseFromFolder(string folderPath)
        {
            try
            {
                var directories = Directory.GetDirectories(folderPath);
                if (directories.Length == 0)
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);
                    var Files = directoryInfo.GetFiles("*.mp3");

                    foreach (var file in Files)
                    {
                        //var info = await GetMusicProperties(file.FullName);
                        var aws = await AudioInfoDownloader.GetTrackInfoByFingerPrint(file.FullName);

                        //    if (info.Album != "" && info.Artist != "")
                        //    {
                        //        var album = await AudioInfoDownloader.UpdateDatabase(info.Artist, info.Album);

                        //        if (album != null)
                        //            Console.WriteLine($"Album {album.Name} was sucessfuly added to database");

                        //        if (info.Title != "")
                        //        {
                        //            await AudioInfoDownloader.SetDiskLocationToSong(info.Title, info.Album, info.Artist, file.FullName);
                        //            Console.WriteLine($"Song {info.Title} was updated in database");
                        //        }
                        //    }
                    }
                }
                else
                {
                    for (int i = 0; i < directories.Length; i++)
                    {
                        await UpdateDatabaseFromFolder(directories[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        private async Task<KeyValuePair<StorageFile, MusicProperties>> GetMusicProperties(string path)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(path);
            MusicProperties musicProperties = await file.Properties.GetMusicPropertiesAsync();
            
            return new KeyValuePair<StorageFile, MusicProperties>(file,musicProperties);
        }

       
    }
}
