using System;
using System.Collections.ObjectModel;
using VCore.ViewModels;
using VPlayer.Core.Design;
using VPlayer.Core.Design.ViewModels;
using VPlayer.Core.DomainClasses;
using VPlayer.Core.ViewModels;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Library.ViewModels.AlbumsViewModels;
using VPlayer.Player.ViewModels;

namespace VPlayer.Player.Design
{
  public class PlayerDesingViewModel : ViewModel
  {
    public PlayerDesingViewModel()
    {
      PlayList = new ObservableCollection<SongInPlayListDesing>();

      foreach (var song in DesingDatabase.Instance.Albums[0].Songs)
      {
        PlayList.Add(new SongInPlayListDesing(song));
      }

      PlayList[2].IsPlaying = true;
      ActualSong = PlayList[2];
      ActualSong.ActualPosition = (float)0.37;

      SelectedSong = PlayList[3];
    }

    public SongInPlayListDesing ActualSong { get; set; }
    public SongInPlayListDesing SelectedSong { get; set; }
    public ObservableCollection<SongInPlayListDesing> PlayList { get; set; }
  }

  public class SongInPlayListDesing : ViewModel<Song>
  {
    public TimeSpan ActualTime => TimeSpan.FromSeconds(ActualPosition * Duration);
    public float ActualPosition { get; set; }
    public string Name => Model.Name;
    public double Duration => Model.Duration;

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

    public byte[] Image => Model?.Album.AlbumFrontCoverBLOB;
    public AlbumDesignViewModel AlbumViewModel { get; set; }
    public ArtistDesignViewModel ArtistViewModel { get; set; }

    public SongInPlayListDesing(Song model) : base(model)
    {
      AlbumViewModel = new AlbumDesignViewModel(model.Album);
      ArtistViewModel = new ArtistDesignViewModel(AlbumViewModel.Model.Artist);
    }
  }
}
