using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;
using Ninject;
using VPlayer.Player.ViewModels;

namespace VPlayer.Player.Behaviors
{
  public class SliderValueChangedManualy : Behavior<Slider>
  {
    #region Kernel

    public IKernel Kernel
    {
      get { return (IKernel)GetValue(KernelProperty); }
      set { SetValue(KernelProperty, value); }
    }

    public static readonly DependencyProperty KernelProperty =
      DependencyProperty.Register(
        nameof(Kernel),
        typeof(IKernel),
        typeof(SliderValueChangedManualy),
        new PropertyMetadata(null));


    #endregion

    protected override void OnAttached()
    {
      base.OnAttached();

      AssociatedObject.ValueChanged += AssociatedObject_ValueChanged;
    }

    private void AssociatedObject_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
      if (Mouse.LeftButton == MouseButtonState.Pressed && AssociatedObject.IsMouseOver)
      {
        if (Kernel != null)
          Kernel.Get<PlayerViewModel>().MediaPlayer.Position = (float)e.NewValue;
      }
    }

    protected override void OnDetaching()
    {
      base.OnDetaching();

      AssociatedObject.ValueChanged -= AssociatedObject_ValueChanged;
    }
  }
}
