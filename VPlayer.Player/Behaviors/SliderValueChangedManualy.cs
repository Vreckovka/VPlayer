using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interactivity;
using Ninject;
using VCore.Controls;
using VPlayer.Player.ViewModels;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Vlc.DotNet.Core.Interops.Signatures;

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
