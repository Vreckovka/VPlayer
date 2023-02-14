using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using VCore.WPF.Behaviors;
using VCore.WPF.Managers;

namespace VPlayer.WindowsPlayer.Vlc.Themes
{
  public class PopupOpenedHookToSliderBehavior : Behavior<Popup>
  {
    #region Slider

    public Slider Slider
    {
      get { return (Slider)GetValue(SliderProperty); }
      set { SetValue(SliderProperty, value); }
    }

    public static readonly DependencyProperty SliderProperty =
      DependencyProperty.Register(
        nameof(Slider),
        typeof(Slider),
        typeof(PopupOpenedHookToSliderBehavior),
        new PropertyMetadata(null));


    #endregion

    protected override void OnAttached()
    {
      base.OnAttached();

      AssociatedObject.Opened += AssociatedObject_Opened;
    }

    private void AssociatedObject_Opened(object sender, EventArgs e)
    {
      if (Slider != null)
      {
        VFocusManager.SetFocus(Slider);
        //Slider.Focus();
      }
       
    }

    protected override void OnDetaching()
    {
      base.OnDetaching();

      AssociatedObject.Opened -= AssociatedObject_Opened;
    }
  }
}
