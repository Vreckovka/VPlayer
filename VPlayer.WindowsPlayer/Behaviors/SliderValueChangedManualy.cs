using Ninject;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;
using VPlayer.Player.ViewModels;

namespace VPlayer.Player.Behaviors
{
  public class SliderValueChangedManualy : Behavior<Slider>
  {
    #region Kernel

    public static readonly DependencyProperty KernelProperty =
      DependencyProperty.Register(
        nameof(Kernel),
        typeof(IKernel),
        typeof(SliderValueChangedManualy),
        new PropertyMetadata(null));

    public IKernel Kernel
    {
      get { return (IKernel)GetValue(KernelProperty); }
      set { SetValue(KernelProperty, value); }
    }

    #endregion Kernel

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
    }

    protected override void OnDetaching()
    {
      base.OnDetaching();

      AssociatedObject.ValueChanged -= AssociatedObject_ValueChanged;
    }

    private void AssociatedObject_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
      if (Mouse.LeftButton == MouseButtonState.Pressed && AssociatedObject.IsMouseOver)
      {
        if (Kernel != null) Kernel.Get<WindowsPlayerViewModel>().MediaPlayer.Position = (float)e.NewValue;
      }
    }

    #endregion Methods
  }
}