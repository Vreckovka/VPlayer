using System;
using System.Windows.Input;
using VCore;
using VCore.Standard;

namespace VPlayer.Core.ViewModels.SoundItems.LRCCreators
{
  public class LRCCreatorLyricsLine : ViewModel
  {
    #region Text

    private string text;

    public string Text
    {
      get { return text; }
      set
      {
        if (value != text)
        {
          text = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Time

    private TimeSpan? time;

    public TimeSpan? Time
    {
      get { return time; }
      set
      {
        if (value != time)
        {
          time = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

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

    #region SetTime

    private ActionCommand setTime;

    public ICommand SetTime
    {
      get
      {
        if (setTime == null)
        {
          setTime = new ActionCommand(OnSetTime);
        }

        return setTime;
      }
    }


    private void OnSetTime()
    {
      IsActual = false;
    }

    #endregion

    public void SetIsActual(bool value)
    {
      isActual = value;
    }

  }
}