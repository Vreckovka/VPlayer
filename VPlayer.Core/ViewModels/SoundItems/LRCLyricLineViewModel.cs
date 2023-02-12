using VCore.Standard;
using VCore.WPF.LRC.Domain;

namespace VPlayer.Core.ViewModels.SoundItems
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

    public void RaiseNotification()
    {
      RaisePropertyChanged(nameof(Text));
      RaisePropertyChanged(nameof(Model));
    }
  }
}