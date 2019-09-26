using System;
using System.Linq;
using VCore.ViewModels;
using VPlayer.Core.DomainClasses;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Library.ViewModels.AlbumsViewModels;

namespace VPlayer.Core.ViewModels
{
  public class SongInPlayList : ViewModel<Song>
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

    public SongInPlayList(IAlbumsViewModel albumsViewModel, IArtistsViewModel artistsViewModel, Song model) : base(model)
    {
      Name = model.Name;
      Duration = TimeSpan.FromSeconds(model.Duration);
      Image = model?.Album.AlbumFrontCoverBLOB;

      AlbumViewModel = albumsViewModel.ViewModels.Single(x => x.ModelId == model.Album.Id);
      ArtistViewModel = artistsViewModel.ViewModels.Single(x => x.ModelId == AlbumViewModel.Model.Artist.Id);
    }
  }
}