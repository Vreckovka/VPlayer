using VCore.ViewModels;
using VPlayer.Core.DomainClasses;

namespace VPlayer.Core.Design.ViewModels
{
  public class AlbumDesignViewModel : ViewModel<Album>
  {
    public string Name => Model.Name;
    public string HeaderText => Model.Name;
    public string BottomText => $"{Model.Artist?.Name}\nNumber of song: {Model.Songs?.Count.ToString()}";
    public byte[] ImageThumbnail => Model.AlbumFrontCoverBLOB;
    public bool IsPlaying { get; set; }

    public AlbumDesignViewModel(Album model) : base(model)
    {
    }
  }
}