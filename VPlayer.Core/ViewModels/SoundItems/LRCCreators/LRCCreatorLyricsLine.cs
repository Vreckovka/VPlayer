using System;
using System.Windows.Input;
using VCore;
using VCore.Standard;
using VCore.WPF.Misc;

namespace VPlayer.Core.ViewModels.SoundItems.LRCCreators
{
  public class LRCCreatorLyricsLine : ViewModel
  {
    public int Index { get; set; }

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

    #region IsInEditMode

    private bool isInEditMode;

    public bool IsInEditMode
    {
      get { return isInEditMode; }
      set
      {
        if (value != isInEditMode)
        {
          isInEditMode = value;
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

          if (time != TimeSpan.Zero)
            StringTime = time.ToString();

          RaisePropertyChanged();
          RaisePropertyChanged(nameof(StringTime));
        }
      }
    }

    #endregion

    #region StringTime

    private string stringTime = "00:00:00.00";

    public string StringTime
    {
      get { return stringTime; }
      set
      {
        if (value != stringTime)
        {
          stringTime = value;

          if (TimeSpan.TryParse(value, out var parsedTime))
            Time = parsedTime;


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