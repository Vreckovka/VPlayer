using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VCore.Standard.Modularity.Interfaces;

namespace VPlayer.WindowsPlayer.Views.WindowsPlayer.IPTV
{
  /// <summary>
  /// Interaction logic for IPTVPlayerView.xaml
  /// </summary>
  public partial class IPTVPlayerView : UserControl, IView
  {
    public IPTVPlayerView()
    {
      InitializeComponent();
      Loaded += IPTVPlayerView_Loaded;
    }

    private void IPTVPlayerView_Loaded(object sender, RoutedEventArgs e)
    {
      FullScreenBehavior.VideoView = VideoView;
      FullScreenBehavior.VideoMenu = VideoMenu;
    }
  }
}
