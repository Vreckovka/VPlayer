using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KeyListener;
using Prism.Events;
using VCore.Annotations;
using VCore.Modularity.RegionProviders;
using VCore.ViewModels;
using Vlc.DotNet.Core;
using VPlayer.Core.DomainClasses;
using VPlayer.Core.Events;
using VPlayer.Core.Modularity.Regions;
using VPlayer.Core.ViewModels;
using VPlayer.Library.ViewModels.AlbumsViewModels;
using VPlayer.Player.Views;

namespace VPlayer.Player.ViewModels
{
  public class SongInPlayList : ViewModel<Song>
  {
    public TimeSpan ActualTime => TimeSpan.FromSeconds(ActualPosition * Duration.TotalSeconds);
    public float ActualPosition { get; set; }
    public string Name { get; set; }
    public TimeSpan Duration { get; set; }
    public bool IsPlaying { get; set; }
    public byte[] Image { get; set; }
    public AlbumViewModel AlbumViewModel { get; set; }
    public ArtistViewModel ArtistViewModel { get; set; }

    public SongInPlayList(Song model) : base(model)
    {
      Name = model.Name;
      Duration = TimeSpan.FromSeconds(model.Duration);
      Image = model?.Album.AlbumFrontCoverBLOB;
    }
  }

  public class PlayerViewModel : RegionCollectionViewModel
  {
    private readonly IEventAggregator eventAggregator;
    public List<SongInPlayList> PlayList { get; set; }
    private int actualSongIndex = 0;
    public VlcMediaPlayer MediaPlayer { get; private set; }
    public double ActualTime { get; set; }
    public SongInPlayList ActualSong { get; private set; }
    public static bool IsPlaying { get; set; }
    public byte[] ActualImage { get; set; }
    public PlayerViewModel(IRegionProvider regionProvider, [NotNull] IEventAggregator eventAggregator) : base(regionProvider)
    {
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

      RegistredViews.Add(typeof(PlayerView), new Tuple<string, bool>(RegionNames.PlayerRegion, false));
      RegistredViews.Add(typeof(WindowsPlayerView), new Tuple<string, bool>(RegionNames.WindowsPlayerContentRegion, false));
    }

    public IEnumerable<SelfActivableNavigationItem> SelfActivableNavigationItems;

    private void PlaySongs(IEnumerable<Song> songs)
    {
      PlayList = songs.Select(x => new SongInPlayList(x)).ToList();
      Play();
    }

    #region Initialize

    public override Dictionary<Type, Tuple<string, bool>> RegistredViews { get; set; } = new Dictionary<Type, Tuple<string, bool>>();

    public override void Initialize()
    {
      base.Initialize();

      KeyListener.KeyListener.OnKeyPressed += KeyListener_OnKeyPressed;

      Views[typeof(WindowsPlayerView)].Header = "Player";


      var currentAssembly = Assembly.GetEntryAssembly();
      var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
      // Default installation path of VideoLAN.LibVLC.Windows
      var path = new DirectoryInfo(Path.Combine(currentDirectory, "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));

      var libDirectory = new DirectoryInfo(path.FullName);
      MediaPlayer = new VlcMediaPlayer(libDirectory);

      bool playFinished = false;

      MediaPlayer.EncounteredError += (sender, e) =>
      {
        Console.Error.Write("An error occurred");
        playFinished = true;
      };

      MediaPlayer.EndReached += (sender, e) => { PlayNext(); };
      MediaPlayer.TimeChanged += MediaPlayer_TimeChanged;
      eventAggregator.GetEvent<PlaySongsEvent>().Subscribe(PlaySongs);
      eventAggregator.GetEvent<PauseEvent>().Subscribe(Pause);
    }

    private void MediaPlayer_TimeChanged(object sender, VlcMediaPlayerTimeChangedEventArgs e)
    {
      ActualSong.ActualPosition = ((VlcMediaPlayer)sender).Position;
      ActualTime = ((VlcMediaPlayer)sender).Position;
    }

    #endregion

    #region Play

    /// <summary>
    /// Plays playlist or play actual song
    /// </summary>
    public void Play()
    {
      Task.Run(() =>
      {
        if (PlayList.Count > 0)
        {
          foreach (var playingSongs in PlayList.Where(x => x.IsPlaying = false))
          {
            playingSongs.IsPlaying = false;
          }

          if (PlayList[actualSongIndex] == ActualSong)
          {
            //mediaPlayer.SetMedia(new Uri(PlayList.Peek().DiskLocation));
            MediaPlayer.Play();
          }
          else
          {
            MediaPlayer.SetMedia(new Uri(PlayList[actualSongIndex].Model.DiskLocation));
            MediaPlayer.Play();
            ActualSong = PlayList[actualSongIndex];
            ActualSong.IsPlaying = true;
            Application.Current.Dispatcher.Invoke(() =>
                  {
                    ActualImage = ActualSong.Model?.Album.AlbumFrontCoverBLOB;
                  });
          }
        }
      });
    }

    #endregion

    public ImageSource ByteToImage(byte[] imageData)
    {
      BitmapImage biImg = new BitmapImage();
      MemoryStream ms = new MemoryStream(imageData);
      biImg.BeginInit();
      biImg.StreamSource = ms;
      biImg.EndInit();

      ImageSource imgSrc = biImg as ImageSource;

      return imgSrc;
    }

    public void Pause()
    {
      MediaPlayer.Pause();
    }

    public void PlayNext()
    {
      actualSongIndex++;
      Play();
    }

    private void KeyListener_OnKeyPressed(object sender, KeyPressedArgs e)
    {
      if (e.KeyPressed == Key.MediaPlayPause)
      {
        if (IsPlaying)
        {
          IsPlaying = false;
          Play();
        }
        else
        {
          IsPlaying = true;
          Pause();
        }
      }
      else if (e.KeyPressed == Key.MediaNextTrack)
      {
        PlayNext();
      }
    }
  }
}

