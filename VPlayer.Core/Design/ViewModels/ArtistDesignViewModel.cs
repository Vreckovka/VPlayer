using VCore.Standard;
using VCore.ViewModels;
using VPlayer.AudioStorage.DomainClasses;

namespace VPlayer.Core.Design.ViewModels
{
  public class ArtistDesignViewModel : ViewModel<Artist>
  {
    #region Constructors

    public ArtistDesignViewModel(Artist model) : base(model)
    {
    }

    #endregion Constructors

    #region Properties

    public bool IsPlaying { get; set; }
    public string Name => Model.Name;

    #endregion Properties
  }
}