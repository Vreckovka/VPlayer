using System;
using Prism.Regions;
using VCore.Factories;
using VCore.Modularity.Interfaces;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Library.ViewModels;
using VPlayer.Library.Views;
using VPlayer.Player.ViewModels;
using VPlayer.WindowsPlayer.Annotations;
using VPlayer.WindowsPlayer.Views;

namespace VPlayer.WindowsPlayer.ViewModels
{
    public class WindowsPlayerViewModel : RegionViewModel<WindowsPlayerView>, INavigationItem
    {
        private readonly IViewModelsFactory viewModelsFactory;

        public WindowsPlayerViewModel(
          IRegionProvider regionProvider,
          IViewModelsFactory viewModelsFactory,
          NavigationViewModel navigationViewModel) : base(regionProvider)
        {
            this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
            NavigationViewModel = navigationViewModel ?? throw new ArgumentNullException(nameof(navigationViewModel));
        }

        public string Header => "Windows player";
        public override string RegionName => RegionNames.ContentRegion;
        public override bool ContainsNestedRegions => true;
        public NavigationViewModel NavigationViewModel { get; set; }


        public override void Initialize()
        {
            base.Initialize();

            var libraryViewModel = viewModelsFactory.Create<LibraryViewModel>();
            libraryViewModel.IsActive = true;

            var playerViewModel = viewModelsFactory.Create<PlayerViewModel>();
            var item = playerViewModel.Views[typeof(Player.Views.WindowsPlayer.WindowsPlayerView)];

            NavigationViewModel.Items.Add(libraryViewModel);
            NavigationViewModel.Items.Add(item);
        }

        //public override  (IContainerProvider containerProvider)
        //{
        //    var regionManager = containerProvider.Resolve<IRegionManager>();

        //    regionManager.RegisterViewWithRegion("MainRegion", typeof(WindowsPlayerView));
        //}

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
