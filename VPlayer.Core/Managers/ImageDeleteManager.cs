using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Ninject;
using Prism.Events;
using Prism.Regions;
using VCore.Controls;
using VCore.Helpers;
using VCore.Modularity.RegionProviders;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.Core.Interfaces.ViewModels;
using VPlayer.Core.Messages.ImageDelete;
using VPlayer.Library.ViewModels.AlbumsViewModels;

namespace VPlayer.Core.Managers
{
  public static class ImageDeleteManager
  {
    private static IEventAggregator eventAggregator;
    private static IRegionProvider regionProvider;
    private static IKernel kernel;

    private static IAlbumsViewModel albumsViewModel;
    private static IArtistsViewModel artistsViewModel;

    private static Dictionary<Album, PlayableWrapPanelItem> Albums = new Dictionary<Album, PlayableWrapPanelItem>();

    public static void Initlize(IEventAggregator pEventAggregator, IKernel pKernel, IRegionProvider pRegionProvider)
    {
      kernel = pKernel;
      regionProvider = pRegionProvider;
      eventAggregator = pEventAggregator;

      eventAggregator.GetEvent<ImageDeleteRequestEvent>().Subscribe(OnImageDelete);
    }

    public static ImageDeleteRequestEvent AddToDelete(PlayableWrapPanelItem image, Album album)
    {
      if (!Albums.TryGetValue(album, out var x))
      {
        Albums.Add(album, image);
      }

      if (albumsViewModel == null)
      {
        albumsViewModel = kernel.Get<IAlbumsViewModel>();
      }

      if (artistsViewModel == null)
      {
        artistsViewModel = kernel.Get<IArtistsViewModel>();
      }

      return eventAggregator.GetEvent<ImageDeleteRequestEvent>();
    }

    private static void OnImageDelete(ImageDeleteRequestEventArgs imageDeleteDoneEventArgs)
    {
      Application.Current?.Dispatcher?.Invoke(() =>
      {
        bool success = false;

        Album model = imageDeleteDoneEventArgs.Model;

        try
        {
          if (Albums.TryGetValue(model, out var playableWrapPanelItem))
          {
            DeletePictureSource(playableWrapPanelItem);

            success = true;
          }

          albumsViewModel.RecreateCollection();
          artistsViewModel.RecreateCollection();

          System.GC.Collect();
          System.GC.WaitForPendingFinalizers();
        }
        catch (Exception ex)
        {
          success = false;
          throw;
        }
        finally
        {
          eventAggregator.GetEvent<ImageDeleteDoneEvent>().Publish(new ImageDeleteDoneEventArgs()
          {
            Model = imageDeleteDoneEventArgs.Model,
            Result = success
          });
        }
      });
    }

    private static void DeletePictureSource(PlayableWrapPanelItem playableWrapPanelItem)
    {
      var datacontext = playableWrapPanelItem.DataContext;

      playableWrapPanelItem.ImageThumbnail = null;
      playableWrapPanelItem.DataContext = null;

      var image = playableWrapPanelItem.GetFirstChildOfType<Image>();
      image.Source = null;
      image.UpdateLayout();

      playableWrapPanelItem.UpdateLayout();

      playableWrapPanelItem.DataContext = datacontext;
      
    }

  }
}