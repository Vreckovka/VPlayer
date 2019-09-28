using System;
using System.Linq;
using System.Windows.Input;
using Prism.Events;
using VCore;
using VCore.Annotations;
using VCore.ViewModels;
using VPlayer.Core.DomainClasses;
using VPlayer.Core.Events;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Library.ViewModels.AlbumsViewModels;

namespace VPlayer.Core.ViewModels
{
  public class SongInPlayList : ViewModel<Song>
  {
    private readonly IEventAggregator eventAggregator;
    public TimeSpan ActualTime => TimeSpan.FromSeconds(ActualPosition * Duration);
    public float ActualPosition { get; set; }
    public string Name => Model.Name;
    public int Duration => Model.Duration;
    
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

    #endregion

    public bool IsPaused { get; set; }
    public byte[] Image => AlbumViewModel.Model?.AlbumFrontCoverBLOB;
    public AlbumViewModel AlbumViewModel { get; set; }
    public ArtistViewModel ArtistViewModel { get; set; }

    public SongInPlayList([NotNull] IEventAggregator eventAggregator, IAlbumsViewModel albumsViewModel, IArtistsViewModel artistsViewModel, Song model) : base(model)
    {
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

      AlbumViewModel = albumsViewModel.ViewModels.Single(x => x.ModelId == model.Album.Id);
      ArtistViewModel = artistsViewModel.ViewModels.Single(x => x.ModelId == AlbumViewModel.Model.Artist.Id);
    }

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

    public void OnPlay()
    {
      eventAggregator.GetEvent<PlaySongsInPlayListEvent>().Publish(this);
    }
    #endregion
  }
}