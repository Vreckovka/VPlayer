using System.Windows.Controls;
using VCore.Standard.Modularity.Interfaces;

namespace VPlayer.Library.Views
{
  /// <summary>
  /// Interaction logic for PlaylistsView.xaml
  /// </summary>
  public partial class PlaylistsView : UserControl, IView
  {
    public PlaylistsView()
    {
      InitializeComponent();
    }


    //private void PlaylistsView_Loaded(object sender, RoutedEventArgs e)
    //{
    //  var dpd = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ListView));
    //  if (dpd != null)
    //  {
    //    dpd.AddValueChanged(playlists, ThisIsCalledWhenPropertyIsChanged);
    //  }
    //}

    //private void ThisIsCalledWhenPropertyIsChanged(object sender, EventArgs e)
    //{
    //  if (playlists.ItemsSource != null)
    //  {
    //    CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(playlists.ItemsSource);
    //    PropertyGroupDescription groupDescription = new PropertyGroupDescription(nameof(PlaylistViewModel.IsUserCreated));
    //    view.GroupDescriptions.Add(groupDescription);
    //  }
    //}


  }
}
