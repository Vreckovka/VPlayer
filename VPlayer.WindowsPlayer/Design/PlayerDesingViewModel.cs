using System;
using System.Collections.ObjectModel;
using VCore.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.Core.Design;
using VPlayer.Core.Design.ViewModels;

namespace VPlayer.Player.Design
{
  public class PlayerDesingViewModel : ViewModel
  {
    #region Constructors

    public PlayerDesingViewModel()
    {
      PlayList = new ObservableCollection<SongInPlayListDesing>();

      foreach (var song in DesingDatabase.Instance.Albums[0].Songs)
      {
        PlayList.Add(new SongInPlayListDesing(song));
      }

      PlayList[2].IsPlaying = true;
      ActualSong = PlayList[2];
      ActualSong.ActualPosition = (float) 0.37;

      SelectedSong = PlayList[3];
    }

    #endregion Constructors

    #region Properties

    public SongInPlayListDesing ActualSong { get; set; }
    public ObservableCollection<SongInPlayListDesing> PlayList { get; set; }
    public SongInPlayListDesing SelectedSong { get; set; }

    #endregion Properties
  }

  public class SongInPlayListDesing : ViewModel<Song>
  {
    #region Constructors

    public SongInPlayListDesing(Song model) : base(model)
    {
      AlbumViewModel = new AlbumDesignViewModel(model.Album);
      ArtistViewModel = new ArtistDesignViewModel(AlbumViewModel.Model.Artist);
    }

    #endregion Constructors

    #region Properties

    public float ActualPosition { get; set; }
    public TimeSpan ActualTime => TimeSpan.FromSeconds(ActualPosition * Duration);
    public AlbumDesignViewModel AlbumViewModel { get; set; }
    public ArtistDesignViewModel ArtistViewModel { get; set; }
    public double Duration => Model.Duration;
    public string ImagePath => Model?.Album.AlbumFrontCoverFilePath;
    public string Name => Model.Name;

    #endregion Properties

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
  }
}