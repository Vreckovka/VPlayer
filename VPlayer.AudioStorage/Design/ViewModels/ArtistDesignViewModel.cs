using VCore.ViewModels;
using VPlayer.Core.DomainClasses;

namespace VPlayer.Core.Design.ViewModels
{
  public class ArtistDesignViewModel : ViewModel<Artist>
  {
    public string Name => Model.Name;

    public bool IsPlaying { get; set; }

    public ArtistDesignViewModel(Artist model) : base(model)
    {
    }
  }
}