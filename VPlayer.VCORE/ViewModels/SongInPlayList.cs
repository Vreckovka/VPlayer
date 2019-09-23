using System;
using VCore.ViewModels;
using VPlayer.Core.DomainClasses;
using VPlayer.Library.ViewModels.AlbumsViewModels;

namespace VPlayer.Core.ViewModels
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
}