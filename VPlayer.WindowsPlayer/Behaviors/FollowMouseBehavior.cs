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
  public class ShowPopupBehavior : Behavior<Slider>
  {
    #region Popup

    public static readonly DependencyProperty PopupProperty =
      DependencyProperty.Register(
        nameof(Popup),
        typeof(Popup),
        typeof(ShowPopupBehavior),
        new PropertyMetadata(null));

    public Popup Popup
    {
      get { return (Popup)GetValue(PopupProperty); }
      set { SetValue(PopupProperty, value); }
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
      AssociatedObject.Loaded += AssociatedObject_Loaded;

    }

    private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
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
      return AssociatedObject.Maximum * horizontalOffset / AssociatedObject.ActualWidth;
    }
  }
}
