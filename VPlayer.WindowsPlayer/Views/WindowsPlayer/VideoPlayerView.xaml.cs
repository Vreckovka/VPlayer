using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using VPlayer.Core.ViewModels;
using VPlayer.WindowsPlayer.ViewModels;

namespace VPlayer.WindowsPlayer.Views.WindowsPlayer
{
  /// <summary>
  /// Interaction logic for VideoPlayerView.xaml
  /// </summary>
  public partial class VideoPlayerView : UserControl, IView
  {
    public VideoPlayerView()
    {
      InitializeComponent();

      FullScreenBehavior.VideoView = VideoView;
      FullScreenBehavior.FullscreenPlayer = FullscreenPlayer;
    }
  }
}
