using Prism.Events;
using System;
using System.Linq;
using System.Windows.Input;
using VCore;
using VCore.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.Core.Events;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Library.ViewModels.AlbumsViewModels;

namespace VPlayer.Core.ViewModels
{
  public class SongInPlayList : ViewModel<Song>
  {
    #region Fields

    private readonly IEventAggregator eventAggregator;

    #endregion Fields

    #region Constructors

    public SongInPlayList(
      IEventAggregator eventAggregator,
      IAlbumsViewModel albumsViewModel,
      IArtistsViewModel artistsViewModel,
      Song model) : base(model)
    {
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

      AlbumViewModel = albumsViewModel.ViewModels.Single(x => x.ModelId == model.Album.Id);
      ArtistViewModel = artistsViewModel.ViewModels.Single(x => x.ModelId == AlbumViewModel.Model.Artist.Id);
    }

    #endregion Constructors

    #region Properties

    public float ActualPosition { get; set; }
    public TimeSpan ActualTime => TimeSpan.FromSeconds(ActualPosition * Duration);
    public AlbumViewModel AlbumViewModel { get; set; }
    public ArtistViewModel ArtistViewModel { get; set; }
    public int Duration => Model.Duration;
    public byte[] Image => AlbumViewModel.Model?.AlbumFrontCoverBLOB;
    public bool IsPaused { get; set; }
    public string Name => Model.Name;

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

    #endregion Properties
  }
}