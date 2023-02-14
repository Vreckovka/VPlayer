using System;
using System.Windows.Controls;
using System.Windows.Interactivity;
using System.Windows.Media.Imaging;

namespace VPlayer.WindowsPlayer.Behaviors
{
  public class ImageToGifBehavior : Behavior<Image>
  {
    protected override void OnAttached()
    {
      base.OnAttached();

      AssociatedObject.SourceUpdated += AssociatedObject_SourceUpdated;
      AssociatedObject.Loaded += AssociatedObject_Loaded;
    }

    private void AssociatedObject_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
      var image = new BitmapImage();
      image.BeginInit();
      var ulr = AssociatedObject.Source.ToString();
      image.UriSource = new Uri(ulr,UriKind.Absolute);
      image.EndInit();
      ImageBehavior.SetAnimatedSource(AssociatedObject, image);
    }

    private void AssociatedObject_SourceUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
    {
      //var image = new BitmapImage();
      //image.BeginInit();
      //image.UriSource = new Uri(fileName);
      //image.EndInit();
      //ImageBehavior.SetAnimatedSource(AssociatedObject, image);
    }

    protected override void OnDetaching()
    {
      base.OnDetaching();

      AssociatedObject.SourceUpdated -= AssociatedObject_SourceUpdated;
      AssociatedObject.Loaded -= AssociatedObject_Loaded;
    }
  }
}
