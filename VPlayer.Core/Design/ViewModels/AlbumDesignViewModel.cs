using VCore.ViewModels;
using VPlayer.AudioStorage.DomainClasses;

namespace VPlayer.Core.Design.ViewModels
{
  public class AlbumDesignViewModel : ViewModel<Album>
  {
    #region Constructors

    public AlbumDesignViewModel(Album model) : base(model)
    {
    }

    #endregion Constructors

    #region Properties

    public string BottomText => $"{Model.Artist?.Name}\nNumber of song: {Model.Songs?.Count.ToString()}";
    public string HeaderText => Model.Name;
    public byte[] ImageThumbnail => Model.AlbumFrontCoverBLOB;
    public bool IsPlaying { get; set; }
    public string Name => Model.Name;

    #endregion Properties
  }
}