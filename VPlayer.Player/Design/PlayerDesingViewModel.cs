using System;
using System.Collections.ObjectModel;
using VCore.ViewModels;
using VPlayer.Core.Desing;
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
    public TimeSpan ActualTime => TimeSpan.FromSeconds(ActualPosition * Duration.TotalSeconds);
    public float ActualPosition { get; set; }
    public string Name { get; set; }
    public TimeSpan Duration { get; set; }

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

    public byte[] Image { get; set; }
    public AlbumViewModel AlbumViewModel { get; set; }
    public ArtistViewModel ArtistViewModel { get; set; }

    public SongInPlayListDesing(Song model) : base(model)
    {
      Name = model.Name;
      Duration = TimeSpan.FromSeconds(model.Duration);
      Image = model?.Album.AlbumFrontCoverBLOB;
    }
  }
}
