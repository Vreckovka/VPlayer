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
//using Windows.Storage;
//using Windows.Storage.FileProperties;
//using VPlayer.LocalMusicDatabase;
//using PropertyChanged;
//using VPlayer.Other;
//using VPlayer.Other.AudioInfoDownloader;


using VPlayer.Models;
using VPlayer.WindowsPlayer.Models;
using FileAttributes = System.IO.FileAttributes;

namespace VPlayer.ViewModels
{
    public class WindowsPlayerViewModel 
    {
        public ObservableCollection<AudioTrack> AudioTracks { get; } = new ObservableCollection<AudioTrack>();
        public AudioTrack ActualTrack { get; set; }
        public bool IsPlaying { get; set; }
        public TimeSpan ActualTime { get; set; } = new TimeSpan();
        private int _actualTrackId = 0;
        public bool ManualChanged { get; set; }
        public WindowsPlayerViewModel()
        {

        }

        //public void Play(Uri uri = null, bool next = false)
        //{
        //    if (next)
        //    {
        //        _actualTrackId++;

        //        ActualTrack.IsPlaying = false;

        //        ActualTrack = AudioTracks[_actualTrackId];
        //        ActualTrack.IsPlaying = true;
        //    }

        //    if (uri == null)
        //    {
        //        ActualTrack.IsPlaying = true;
        //    }
        //    else
        //    {
        //        ActualTrack.IsPlaying = false;
        //        _actualTrackId = AudioTracks.IndexOf((from x in AudioTracks where x.Uri == uri select x).First());

        //        ActualTrack = AudioTracks[_actualTrackId];

        //        ActualTrack.IsPlaying = true;
        //    }

        //    IsPlaying = true;
        //    PlayerHandler.OnPlay(this);
        //}
        //public void AddFiles(string[] path)
        //{
        //    foreach (var file in path)
        //    {
        //        FileAttributes attr = File.GetAttributes(file);

        //        if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
        //            AddFolder(file);
        //        else
        //            AddFile(file);
        //    }

        //    if (ActualTrack == null && AudioTracks.Count > 0)
        //    {
        //        ActualTrack = AudioTracks[0];
        //    }
        //}
        //public void AddFolder(string folderPath)
        //{
        //    var directories = Directory.GetDirectories(folderPath);

        //    if (directories.Length == 0)
        //    {
        //        DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);
        //        var Files = directoryInfo.GetFiles("*.mp3");

        //        foreach (var file in Files)
        //        {
        //            AddFile(file.FullName);
        //        }
        //    }
        //    else
        //    {
        //        for (int i = 0; i < directories.Length; i++)
        //        {
        //            AddFolder(directories[i]);
        //        }
        //    }
        //}
        //public void AddFile(string filePath)
        //{
        //    var tagLib = TagLib.File.Create(filePath);

        //    AudioTrack audioTrack = new AudioTrack()
        //    {
        //        Uri = new Uri(filePath),
        //        Name = tagLib.Tag.Title,
        //        Duration = tagLib.Properties.Duration,
        //        Artist = tagLib.Tag.FirstAlbumArtist,
        //    };

        //    if (tagLib.Tag.Title == null)
        //    {
        //        audioTrack.Name = GetFileName(tagLib);
        //    }

        //    Application.Current.Dispatcher.Invoke(() => { AudioTracks.Add(audioTrack); });

        //}
        //public string GetFileName(TagLib.File file)
        //{
        //    int temp = file.Name.LastIndexOf('\\');
        //    return file.Name.Substring(temp + 1, file.Name.Length - temp - 1);
        //}
        //public async Task UpdateDatabaseFromFolder(string folderPath)
        //{
        //    try
        //    {
        //        var directories = Directory.GetDirectories(folderPath);
        //        if (directories.Length == 0)
        //        {
        //            DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);
        //            var extensions = new[] { "*.mp3", "*.wav" };
        //            var files = extensions.SelectMany(ext => directoryInfo.GetFiles(ext));

        //            foreach (var file in files)
        //            {
        //                AudioInfo audioInfo = null;
        //                audioInfo = await AudioInfoDownloader.GetSongInfoFromFileAsync(file.FullName);

        //                if (audioInfo == null)
        //                    audioInfo = await AudioInfoDownloader.GetTrackInfoByFingerPrint(file.FullName);

        //                if (audioInfo != null)
        //                {
        //                    await DatabaseManager.UpdateDiscLocationOfSong(audioInfo, file.FullName);
        //                }
        //            }

        //            Console.WriteLine("Update done");
        //        }
        //        else
        //        {
        //            for (int i = 0; i < directories.Length; i++)
        //            {
        //                await UpdateDatabaseFromFolder(directories[i]);
        //            }

        //            Console.WriteLine("Update done");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //        throw;
        //    }
        //}

    }
}
