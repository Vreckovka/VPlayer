using VCore.Standard;
using VPlayer.AudioStorage.InfoDownloader.LRC.Domain;

namespace VPlayer.Core.ViewModels
{
  public class LRCLyricLineViewModel : ViewModel<LRCLyricLine>
  {
    public bool IsActual { get; set; }
    public string Text => Model.Text;

    public LRCLyricLineViewModel(LRCLyricLine model) : base(model)
    {
    }
  }
}