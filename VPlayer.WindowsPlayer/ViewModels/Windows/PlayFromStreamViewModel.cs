using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using VCore.Standard;
using VCore.Standard.Helpers;
using VCore.WPF.ViewModels.Prompt;

namespace VPlayer.WindowsPlayer.ViewModels.Windows
{
  public class PlayFromStreamViewModel : PromptViewModel
  {

    public PlayFromStreamViewModel()
    {
      CanExecuteOkCommand = () => !string.IsNullOrEmpty(StreamUrl);
    }

    #region StreamUrl

    private string streamUrl;

    public string StreamUrl
    {
      get { return streamUrl; }
      set
      {
        if (value != streamUrl)
        {
          streamUrl = value;
          okCommand?.RaiseCanExecuteChanged();
          RaisePropertyChanged();
        }
      }
    }

    #endregion

  }
}
