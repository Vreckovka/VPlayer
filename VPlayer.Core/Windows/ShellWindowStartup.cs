using System;
using System.Windows;

namespace VPlayer.Core.Windows
{
  public static class ShellWindowStartup
  {
    // Loaded may bubble from descendants or fire again after reparenting.
    // Startup must not change window ordering after the user switches apps.
    public static void Attach(Window window, Action closeSplash)
    {
      if (window == null) throw new ArgumentNullException(nameof(window));
      if (closeSplash == null) throw new ArgumentNullException(nameof(closeSplash));
      RoutedEventHandler loaded = null;
      loaded = (sender, args) =>
      {
        if (!ReferenceEquals(args.OriginalSource, window)) return;
        window.Loaded -= loaded;
        closeSplash();
      };
      window.Loaded += loaded;
    }
  }
}
