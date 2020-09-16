using Ninject;
using Prism.Events;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using System.Windows.Media;
using VCore.Behaviors.Text;
using VCore.Controls;
using VCore.Helpers;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.Library.ViewModels.AlbumsViewModels;

namespace VPlayer.Library.Behaviors
{
  public class DeletePictureItemBehavior : Behavior<PlayableWrapPanel>
  {
    #region EventAggregator

    public static readonly DependencyProperty EventAggregatorProperty =
      DependencyProperty.Register(
        nameof(EventAggregator),
        typeof(IEventAggregator),
        typeof(DeletePictureItemBehavior),
        new PropertyMetadata(null));

    public IEventAggregator EventAggregator
    {
      get { return (IEventAggregator)GetValue(EventAggregatorProperty); }
      set { SetValue(EventAggregatorProperty, value); }
    }

    #endregion

    protected override void OnAttached()
    {
      base.OnAttached();

      AssociatedObject.Loaded += AssociatedObject_Loaded;
      AssociatedObject.Unloaded += AssociatedObject_Unloaded;
    }

    private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
    {
      subscriptionToken?.Dispose();
    }

    private SubscriptionToken subscriptionToken;
    private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
    {
      subscriptionToken = EventAggregator.GetEvent<ImageDeleteRequestEvent>().Subscribe(OnImageDelete);
    }

    private void OnImageDelete(ImageDeleteRequestEventArgs imageDeleteDoneEventArgs)
    {
      Application.Current?.Dispatcher?.Invoke(() =>
      {
        var items = this.AssociatedObject.GetListViewItemsFromList().ToList();
        var imagePanelItem = items.SingleOrDefault(x => ((AlbumViewModel)x.DataContext).ModelId == imageDeleteDoneEventArgs.Model.Id);

        var image = imagePanelItem?.FindChildrenOfType<Image>().FirstOrDefault();

        if (imagePanelItem != null && image != null)
        {
          imagePanelItem.ImageThumbnail = null;
          image.Source = null;

          AssociatedObject.UpdateLayout();
        }


        EventAggregator.GetEvent<ImageDeleteDoneEvent>().Publish(new ImageDeleteDoneEventArgs()
        {
          Model = imageDeleteDoneEventArgs.Model
        });
      });
    }
  }

  public enum ImageDeleteEventType
  {
    Deleted,
    NotFound
  }

  public class ImageDeleteRequestEventArgs
  {
    public Album Model { get; set; }
  }

  public class ImageDeleteDoneEventArgs : ImageDeleteRequestEventArgs
  {
  }

  public class ImageDeleteDoneEvent : PubSubEvent<ImageDeleteDoneEventArgs>
  {

  }

  public class ImageDeleteRequestEvent : PubSubEvent<ImageDeleteRequestEventArgs>
  {

  }
}
