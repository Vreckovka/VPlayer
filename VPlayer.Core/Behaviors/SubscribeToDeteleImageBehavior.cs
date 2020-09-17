using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using Prism.Events;
using VCore.Controls;
using VCore.Helpers;
using VPlayer.Core.Managers;
using VPlayer.Core.Messages.ImageDelete;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.Library.ViewModels.AlbumsViewModels;

namespace VPlayer.Core.Behaviors
{
  public class SubscribeToDeteleImageBehavior : Behavior<Image>
  {
    protected override void OnAttached()
    {
      base.OnAttached();

      AssociatedObject.Loaded += AssociatedObject_Loaded;
      AssociatedObject.Unloaded += AssociatedObject_Unloaded;
    }

    private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
    {
      AssociatedObject.Loaded -= AssociatedObject_Loaded;
      AssociatedObject.Unloaded -= AssociatedObject_Unloaded;
    }

    private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
    {
      if (AssociatedObject.DataContext is AlbumViewModel viewModel)
      {
        if (viewModel.Model != null)
        {
          var playableWrapPanelItem = AssociatedObject.GetFirstParentOfType<PlayableWrapPanelItem>();

          if (playableWrapPanelItem != null)
          {
            ImageDeleteManager.AddToDelete(playableWrapPanelItem, viewModel.Model);
          }
        }
      }
      else if(AssociatedObject.DataContext is ArtistViewModel artistViewModel)
      {
        if (artistViewModel.Model != null)
        {
          var playableWrapPanelItem = AssociatedObject.GetFirstParentOfType<PlayableWrapPanelItem>();

          var coverAlbum = artistViewModel.Model.Albums.FirstOrDefault();

          if (playableWrapPanelItem != null && coverAlbum != null)
          {
            ImageDeleteManager.AddToDelete(playableWrapPanelItem, coverAlbum);
          }
        }
      }
    }
  }
}
