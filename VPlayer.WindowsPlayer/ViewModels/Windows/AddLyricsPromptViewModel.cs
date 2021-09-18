using System;
using System.Collections.Generic;
using System.Text;
using VCore.ViewModels;
using VCore.WPF.Prompts;

namespace VPlayer.WindowsPlayer.ViewModels.Windows
{
  public class AddLyricsPromptViewModel : BasePromptViewModel
  {
    public AddLyricsPromptViewModel()
    {
      CancelVisibility = System.Windows.Visibility.Visible;
    }

    #region Lyrics

    private string lyrics = "";

    public string Lyrics
    {
      get { return lyrics; }
      set
      {
        if (value != lyrics)
        {
          lyrics = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

  }
}
