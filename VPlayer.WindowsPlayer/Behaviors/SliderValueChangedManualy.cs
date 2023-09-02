using Ninject;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using VPlayer.Core.ViewModels;
using VPlayer.WindowsPlayer.ViewModels;

namespace VPlayer.Player.Behaviors
{
  public class SliderValueChangedManualy : Behavior<Slider>
  {

    #region Duration

    public static readonly DependencyProperty DurationProperty =
      DependencyProperty.Register(
        nameof(Duration),
        typeof(double),
        typeof(SliderValueChangedManualy),
        new PropertyMetadata(null));

    public double Duration
    {
      get { return (double)GetValue(DurationProperty); }
      set { SetValue(DurationProperty, value); }
    }

    #endregion Duration

    #region Fields

    private Semaphore semaphore = new Semaphore(1, 1);

    #endregion Fields

    #region Methods

    protected override void OnAttached()
    {
      base.OnAttached();

      AssociatedObject.ValueChanged += AssociatedObject_ValueChanged;
      AssociatedObject.MouseWheel += AssociatedObject_MouseWheel;
    }

    private void AssociatedObject_MouseWheel(object sender, MouseWheelEventArgs e)
    {
      if (AssociatedObject.DataContext is IFilePlayableRegionViewModel playableRegionViewModel)
      {
        if (e.Delta > 0)
        {
          playableRegionViewModel.SeekForward();
        }
        else
        {
          playableRegionViewModel.SeekBackward();
        }

        e.Handled = true;
      }
    }

    protected override void OnDetaching()
    {
      base.OnDetaching();

      AssociatedObject.ValueChanged -= AssociatedObject_ValueChanged;
    }

    private void AssociatedObject_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
      if (Mouse.LeftButton == MouseButtonState.Pressed )
      {
        if(AssociatedObject.DataContext is IFilePlayableRegionViewModel playableRegionViewModel && (AssociatedObject.IsMouseOver || !playableRegionViewModel.IsPlaying))
        {
          playableRegionViewModel.SetMediaPosition((float) e.NewValue);
         
        }
      }
    }

    #endregion Methods
  }
}