using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using VCore.Standard;
using VCore.Standard.Modularity.Interfaces;
using VCore.WPF.Managers;
using VCore.WPF.Prompts;
using VCore.WPF.ViewModels.Prompt;
using VPlayer.WindowsPlayer.Behaviors;

namespace VPlayer.Providers
{
  public class VPlayerWindowManager : WindowManager
  {
    protected override void ShowOverlayWindow()
    {
      if (!FullScreenManager.IsFullscreen)
        base.ShowOverlayWindow();
    }
  }
}
