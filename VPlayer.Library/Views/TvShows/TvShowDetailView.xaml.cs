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

namespace VPlayer.Library.Views.TvShows
{
  /// <summary>
  /// Interaction logic for TvShowDetailView.xaml
  /// </summary>
  public partial class TvShowDetailView : UserControl, IView
  {
    public TvShowDetailView()
    {
      InitializeComponent();
    }

    private void ListView_MouseWheel(object sender, MouseWheelEventArgs e)
    {

    }
  }
}
