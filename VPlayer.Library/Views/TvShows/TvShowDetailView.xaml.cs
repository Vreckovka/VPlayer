using System.Windows.Controls;
using System.Windows.Input;
using VCore.Standard.Modularity.Interfaces;

namespace VPlayer.Home.Views.TvShows
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
