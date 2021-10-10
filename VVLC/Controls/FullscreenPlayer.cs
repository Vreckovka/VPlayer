using System;
using System.Windows.Controls;
using System.Windows.Input;
using VCore;

namespace VVLC.Controls
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
    protected override void OnInitialized(EventArgs e)
    {
      base.OnInitialized(e);
    }

    private void DoubleClick()
    {
      OnDoubleClick?.Invoke();
    }
  }
}
