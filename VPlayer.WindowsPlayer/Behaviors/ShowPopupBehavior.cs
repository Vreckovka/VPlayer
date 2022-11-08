using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using Ninject;
using VCore.Standard;
using VPlayer.Core.ViewModels;
using VPlayer.Player.Behaviors;

namespace VPlayer.WindowsPlayer.Behaviors
{
  public class ShowPopupBehavior : Behavior<FrameworkElement>
  {
    #region Popup

    public static readonly DependencyProperty PopupProperty =
      DependencyProperty.Register(
        nameof(Popup),
        typeof(Popup),
        typeof(ShowPopupBehavior),
        new PropertyMetadata(null, (x, y) =>
        {
          if (x is ShowPopupBehavior popupBehavior)
          {
            if (popupBehavior.Popup != null)
            {
              popupBehavior.Initilize();
            }
          }
        }));

    public Popup Popup
    {
      get { return (Popup)GetValue(PopupProperty); }
      set { SetValue(PopupProperty, value); }
    }

    #endregion

    #region Slider

    public static readonly DependencyProperty SliderProperty =
      DependencyProperty.Register(
        nameof(Slider),
        typeof(Slider),
        typeof(ShowPopupBehavior),
        new PropertyMetadata(null));

    public Slider Slider
    {
      get { return (Slider)GetValue(SliderProperty); }
      set { SetValue(SliderProperty, value); }
    }

    #endregion

    #region ViewModel

    public static readonly DependencyProperty ViewModelProperty =
      DependencyProperty.Register(
        nameof(ViewModel),
        typeof(ISliderPopupViewModel),
        typeof(ShowPopupBehavior),
        new PropertyMetadata(null));

    public ISliderPopupViewModel ViewModel
    {
      get { return (ISliderPopupViewModel)GetValue(ViewModelProperty); }
      set { SetValue(ViewModelProperty, value); }
    }

    #endregion


    protected override void OnAttached()
    {
      base.OnAttached();

      AssociatedObject.MouseMove += AssociatedObject_MouseMove;
      AssociatedObject.MouseLeftButtonDown += AssociatedObject_MouseLeftButtonDown;
      AssociatedObject.MouseWheel += AssociatedObject_MouseWheel;
      AssociatedObject.MouseEnter += AssociatedObject_MouseEnter;
      AssociatedObject.MouseLeave += AssociatedObject_MouseLeave; ;
    }

    private void AssociatedObject_MouseLeave(object sender, MouseEventArgs e)
    {
      ViewModel.IsPopupOpened = false;
    }

    private void AssociatedObject_MouseEnter(object sender, MouseEventArgs e)
    {
      ViewModel.IsPopupOpened = true;
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

    private void AssociatedObject_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (Slider.DataContext is IFilePlayableRegionViewModel playableRegionViewModel && ViewModel != null)
      {
        playableRegionViewModel.SetMediaPosition((float)ViewModel.ActualSliderValue);
      }
    }

    private void Initilize()
    {
      Popup.Closed += Popup_Closed;
    }

    private void Popup_Closed(object sender, EventArgs e)
    {
      lastValue = null;
      Popup.HorizontalOffset = 0;
    }

    private double? lastValue = null;
    private void AssociatedObject_MouseMove(object sender, MouseEventArgs e)
    {
      if (Popup != null)
      {
        var mousePosition = e.GetPosition(AssociatedObject);

        if (lastValue != null)
        {
          var diff = mousePosition.X - lastValue;

          Popup.HorizontalOffset += diff.Value;
        }

        var sliderValue = GetSliderValue(mousePosition.X);

        lastValue = mousePosition.X;

        if (ViewModel != null)
        {
          ViewModel.ActualSliderValue = sliderValue;
        }
      }
    }

    private double GetSliderValue(double horizontalOffset)
    {
      if (Slider != null)
        return Slider.Maximum * horizontalOffset / AssociatedObject.ActualWidth;

      return 0;
    }

    protected override void OnDetaching()
    {
      AssociatedObject.MouseMove -= AssociatedObject_MouseMove;
      AssociatedObject.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;
      AssociatedObject.MouseWheel -= AssociatedObject_MouseWheel;
    }
  }
}
