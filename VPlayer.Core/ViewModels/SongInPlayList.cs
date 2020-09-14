using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Windows.Input;
using VCore;
using VCore.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.AudioStorage.InfoDownloader.Clients.MiniLyrics;
using VPlayer.Core.Events;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Library.ViewModels.AlbumsViewModels;

namespace VPlayer.Core.ViewModels
{
  public class SongInPlayList : ViewModel<Song>
  {
    #region Fields

    private readonly IAlbumsViewModel albumsViewModel;
    private readonly IArtistsViewModel artistsViewModel;
    private readonly AudioInfoDownloader audioInfoDownloader;
    private readonly IEventAggregator eventAggregator;

    #endregion Fields

    #region Constructors

    public SongInPlayList(
      IEventAggregator eventAggregator,
      IAlbumsViewModel albumsViewModel,
      IArtistsViewModel artistsViewModel,
      AudioInfoDownloader audioInfoDownloader,
      Song model) : base(model)
    {
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
      this.albumsViewModel = albumsViewModel ?? throw new ArgumentNullException(nameof(albumsViewModel));
      this.artistsViewModel = artistsViewModel ?? throw new ArgumentNullException(nameof(artistsViewModel));
      this.audioInfoDownloader = audioInfoDownloader ?? throw new ArgumentNullException(nameof(audioInfoDownloader));
    }

    #endregion Constructors

    #region Properties

    #region ActualPosition

    private float actualPosition;

    public float ActualPosition
    {
      get { return actualPosition; }
      set
      {
        if (value != actualPosition)
        {
          actualPosition = value;
          RaisePropertyChanged();

          UpdateSyncedLyrics();
        }
      }
    }
    #endregion

    public TimeSpan ActualTime => TimeSpan.FromSeconds(ActualPosition * Duration);
    public AlbumViewModel AlbumViewModel { get; set; }
    public ArtistViewModel ArtistViewModel { get; set; }
    public int Duration => Model.Duration;
    public string ImagePath => AlbumViewModel.Model?.AlbumFrontCoverFilePath;
    public bool IsPaused { get; set; }
    public string Name => Model.Name;

    public string LRCLyrics => Model.LRCLyrics;

    #region Lyrics

    public string Lyrics
    {
      get
      {
        return Model.Chartlyrics_Lyric;
      }
      set
      {
        if (value != Model.Chartlyrics_Lyric)
        {
          Model.Chartlyrics_Lyric = value;
          RaisePropertyChanged(nameof(LyricsObject));
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region LyricsObject

    public object LyricsObject
    {
      get
      {
        if (LRCLyrics == null)
        {
          return Lyrics;
        }
        else
        {
          return LRCFile;
        }
      }
    }

    #endregion

    #region LRCFile

    private LRCFileViewModel lRCFile;

    public LRCFileViewModel LRCFile
    {
      get { return lRCFile; }
      set
      {
        if (value != lRCFile)
        {
          lRCFile = value;
          RaisePropertyChanged(nameof(LyricsObject));
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsPlaying

    private bool isPlaying;

    public bool IsPlaying
    {
      get { return isPlaying; }
      set
      {
        if (value != isPlaying)
        {
          isPlaying = value;

          if (ArtistViewModel != null)
            ArtistViewModel.IsPlaying = isPlaying;

          if (AlbumViewModel != null)
            AlbumViewModel.IsPlaying = isPlaying;

          RaisePropertyChanged();
        }
      }
    }

    #endregion IsPlaying

    #region Play

    private ActionCommand play;

    public ICommand Play
    {
      get
      {
        if (play == null)
        {
          play = new ActionCommand(OnPlayButton);
        }

        return play;
      }
    }

    public void OnPlay()
    {
      eventAggregator.GetEvent<PlaySongsFromPlayListEvent>().Publish(this);
    }

    private void OnPlayButton()
    {
      if (!IsPlaying)
      {
        OnPlay();
      }
      else
      {
        eventAggregator.GetEvent<PauseEvent>().Publish();
      }
    }

    #endregion Play

    #region Refresh

    private ActionCommand refresh;

    public ICommand Refresh
    {
      get
      {
        if (refresh == null)
        {
          refresh = new ActionCommand(OnRefresh);
        }

        return refresh;
      }
    }

    public void OnRefresh()
    {
      TryGetLRCLyrics();
    }


    #endregion 

    #endregion Properties

    #region Commands

    #region DeleteSongFromPlaylist

    private ActionCommand deleteSongFromPlaylist;

    public ICommand DeleteSongFromPlaylist
    {
      get
      {
        if (deleteSongFromPlaylist == null)
        {
          deleteSongFromPlaylist = new ActionCommand(OnDeleteSongFromPlaylist);
        }

        return deleteSongFromPlaylist;
      }
    }

    public void OnDeleteSongFromPlaylist()
    {
      var songs = new System.Collections.Generic.List<Song>() { Model };

      eventAggregator.GetEvent<DeleteSongEvent>().Publish(new DeleteEventArgs()
      {
        DeleteType = DeleteType.SingleFromPlaylist,
        SongsToDelete = songs
      });
    }

    #endregion


    #region DeleteSongFromPlaylist

    private ActionCommand deleteSongFromPlaylistWithAlbum;

    public ICommand DeleteSongFromPlaylistWithAlbum
    {
      get
      {
        if (deleteSongFromPlaylistWithAlbum == null)
        {
          deleteSongFromPlaylistWithAlbum = new ActionCommand(OnDeleteSongFromPlaylistWithAlbum);
        }

        return deleteSongFromPlaylistWithAlbum;
      }
    }

    public void OnDeleteSongFromPlaylistWithAlbum()
    {
      var songs = new System.Collections.Generic.List<Song>() { Model };

      eventAggregator.GetEvent<DeleteSongEvent>().Publish(new DeleteEventArgs()
      {
        DeleteType = DeleteType.AlbumFromPlaylist,
        SongsToDelete = songs
      });
    }

    #endregion

    #endregion

    #region Methods

    #region Initialize

    public override async void Initialize()
    {
      base.Initialize();

      AlbumViewModel = (await albumsViewModel.GetViewModelsAsync()).SingleOrDefault(x => x.ModelId == Model.Album.Id);
      ArtistViewModel = (await artistsViewModel.GetViewModelsAsync()).SingleOrDefault(x => x.ModelId == AlbumViewModel.Model.Artist.Id);

      if (AlbumViewModel != null)
        AlbumViewModel.IsInPlaylist = true;

      if (ArtistViewModel != null)
        ArtistViewModel.IsInPlaylist = true;
    }

    #endregion

    #region Update

    public void Update(Song song)
    {
      Model.Update(song);

      RaisePropertyChanged(nameof(Name));
      RaisePropertyChanged(nameof(Lyrics));
      RaisePropertyChanged(nameof(LyricsObject));
    }

    #endregion

    #region TryGetLRCLyrics

    public void TryGetLRCLyrics()
    {
      if (!string.IsNullOrEmpty(Model.DiskLocation) && LRCLyrics == null)
      {
        var lrc = audioInfoDownloader.TryUpdateSyncedLyrics(System.IO.Path.GetFileNameWithoutExtension(Model.DiskLocation), ArtistViewModel?.Name, Model);

        if (lrc != null)
          LRCFile = new LRCFileViewModel(lrc);


      }
      else if (LRCLyrics != null)
      {
        var parser = new LRCParser();

        var lrc = parser.Parse(LRCLyrics.Split('\n').ToList());

        if (lrc != null)
          LRCFile = new LRCFileViewModel(lrc);
      }
    }

    #endregion

    #region UpdateSyncedLyrics

    private void UpdateSyncedLyrics()
    {
      if (LRCFile != null)
      {
        LRCFile.SetActualLine(ActualTime);
      }
    }

    #endregion

    #endregion
  }

  public class LRCFileViewModel : ViewModel<LRCFile>
  {

    public LRCFileViewModel(LRCFile model) : base(model)
    {
      Lines = model?.Lines.Select(x => new LRCLyricLineViewModel(x)).ToList();

      if (Lines != null)
      {
        var last = Lines.LastOrDefault();
        var first = Lines.FirstOrDefault();

        if (last != null && string.IsNullOrEmpty(last.Text))
        {
          Lines.Add(new LRCLyricLineViewModel(new LRCLyricLine()
          {
            Text = "(End)",
            Timestamp = last.Model.Timestamp + TimeSpan.FromSeconds(1)
          }));
        }

        if (first != null && first.Model.Timestamp > TimeSpan.FromSeconds(0))
        {

          Lines.Insert(0, new LRCLyricLineViewModel(new LRCLyricLine()
          {
            Text = null,
            Timestamp = TimeSpan.FromSeconds(0)
          }));
        }
      }
    }

    #region ActualSongChanged

    private ReplaySubject<int> actualLineSubject = new ReplaySubject<int>(1);

    public IObservable<int> ActualLineChanged
    {
      get { return actualLineSubject.AsObservable(); }
    }

    #endregion

    public List<LRCLyricLineViewModel> Lines { get; }
    public LRCLyricLineViewModel ActualLine { get; private set; }

    private TimeSpan? lastTimestamp;
    private TimeSpan? nextTimestamp;
    private object batton = new object();

    public void SetActualLine(TimeSpan timeSpan)
    {
      lock (batton)
      {
        if (lastTimestamp == null ||
             (lastTimestamp <= timeSpan &&
             nextTimestamp <= timeSpan) || (lastTimestamp > timeSpan))
        {
          if (ActualLine != null)
          {
            ActualLine.IsActual = false;
          }

          var newLine = Lines.Where(x => x.Model.Timestamp <= timeSpan).OrderByDescending(x => x.Model.Timestamp).FirstOrDefault();

          if (newLine != null && ActualLine != newLine)
          {
            newLine.IsActual = true;
            var oldIndex = Lines.IndexOf(newLine);

            if (oldIndex + 1 < Lines.Count)
            {
              var oldTimestamp = Lines[oldIndex].Model.Timestamp;

              var nextTimestampIndex = oldIndex;

              do
              {
                nextTimestampIndex++;

                if (nextTimestampIndex < Lines.Count)
                {
                  nextTimestamp = Lines[nextTimestampIndex].Model.Timestamp;
                }
                else
                {
                  nextTimestamp = null;
                  break;
                }


              } while (nextTimestamp == oldTimestamp);


            }
            else
            {
              nextTimestamp = null;
            }

            lastTimestamp = timeSpan;

          }

          ActualLine = newLine;

          if (newLine != null)
            actualLineSubject.OnNext(Lines.IndexOf(newLine));
        }
      }
    }
  }

  public class LRCLyricLineViewModel : ViewModel<LRCLyricLine>
  {
    public bool IsActual { get; set; }
    public string Text => Model.Text;

    public LRCLyricLineViewModel(LRCLyricLine model) : base(model)
    {
    }
  }
}