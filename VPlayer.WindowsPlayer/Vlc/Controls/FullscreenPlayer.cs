using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VCore;
using VPlayer.WindowsPlayer.Behaviors;

namespace VPlayer.WindowsPlayer.Vlc.Controls
{
  public class FullscreenPlayer : Control
  {
    public Action OnDoubleClick { get; set; }

   
    private ActionCommand doubleClickCommand;
    public ICommand DoubleClickCommand
    {
      get
      {
        if (doubleClickCommand == null)
        {
          doubleClickCommand = new ActionCommand(DoubleClick);
        }
        return doubleClickCommand;
      }
    }

    public FullscreenPlayer()
    {
     
    }

    private void DoubleClick()
    {
      OnDoubleClick?.Invoke();
    }
  }
}
