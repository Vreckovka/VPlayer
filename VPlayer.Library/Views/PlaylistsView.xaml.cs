using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
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
using VCore.Factories;
using VCore.Modularity.Events;
using VCore.Modularity.Interfaces;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Library.ViewModels;

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
