using VCore.Standard;
using VPlayer.AudioStorage.InfoDownloader.LRC.Domain;

namespace VPlayer.Core.ViewModels
{
  public class LRCLyricLineViewModel : ViewModel<LRCLyricLine>
  {
    #region IsActual

    private bool isActual;

    public bool IsActual
    {
      get { return isActual; }
      set
      {
        if (value != isActual)
        {
          isActual = value;
          RaisePropertyChanged();
        }
      }
    }
    
    #endregion

    public string Text => Model.Text;

    public LRCLyricLineViewModel(LRCLyricLine model) : base(model)
    {
    }
  }
}