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
      FullScreenBehavior.VideoMenu = VideoMenu;
      FullScreenBehavior.HideButton = HideButton;
    }

    private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
    {
      int step = 2;
      int max = 300;

      if (VideoView.DataContext != null && VideoView.DataContext is VideoPlayerViewModel videoPlayerView)
      {
        if (e.Delta > 0)
        {
          if (videoPlayerView.MediaPlayer.Volume + step > max)
          {
            videoPlayerView.SetVolumeAndRaiseNotification(max);
          }
          else
            videoPlayerView.SetVolumeAndRaiseNotification(videoPlayerView.MediaPlayer.Volume + step);
        }

        if (e.Delta < 0)
        {
          if (videoPlayerView.MediaPlayer.Volume - step < 0)
          {
            videoPlayerView.SetVolumeAndRaiseNotification(0);
          }
          else
            videoPlayerView.SetVolumeAndRaiseNotification(videoPlayerView.MediaPlayer.Volume - step);
        }
      }
    }
  }
}
