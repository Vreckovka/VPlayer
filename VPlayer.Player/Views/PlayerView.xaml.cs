using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;
using VCore.Standard.Modularity.Interfaces;
using VCore.WPF.Managers;

namespace VPlayer.Player.Views
{
  /// <summary>
  /// Interaction logic for PlayerView.xaml
  /// </summary>
  public partial class PlayerView : UserControl, IView
  {
    public PlayerView()
    {
      InitializeComponent();
    }

    private void Popup_Opened(object sender, System.EventArgs e)
    {
      HookToSlider(WindowsSlider);
    }

    #region Slider_PreviewMouseWheel

    private void Slider_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
      SetSliderValue(sender, e);
    }

    #endregion

    #region SetSliderValue

    private void SetSliderValue(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
      if (sender is Slider slider)
      {
        slider.IsMoveToPointEnabled = false;

        if (e.Delta > 0)
        {
          slider.Value += 5;
        }
        else
        {
          slider.Value -= 5;
        }

        e.Handled = true;
      }
    }

    #endregion

    #region Slider_PreviewMouseUp

    private void Slider_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      if (sender is Slider slider)
      {
        slider.IsMoveToPointEnabled = true;

        if (!IsMouseOver(slider))
        {
          slider.ReleaseMouseCapture();
          VFocusManager.SetFocus(Application.Current.MainWindow);
          e.Handled = true;
          slider.PreviewMouseUp -= Slider_PreviewMouseUp;
        }
      }
    }

    #endregion

    private void HookToSlider(object sender)
    {
      if (sender is Slider slider)
      {
        VFocusManager.SetFocus(slider);
        slider.Focus();
        slider.CaptureMouse();

        slider.PreviewMouseDown += Slider_PreviewMouseUp;
        slider.MouseWheel += Slider_PreviewMouseWheel;
      }
    }

    private void Slider_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      HookToSlider(sender);
    }

    private bool IsMouseOver(FrameworkElement element)
    {
      bool IsMouseOverEx = false;

      VisualTreeHelper.HitTest(element, d =>
        {
          if (d == element)
          {
            IsMouseOverEx = true;
            return HitTestFilterBehavior.Stop;
          }
          else
            return HitTestFilterBehavior.Continue;
        },
        ht => HitTestResultBehavior.Stop,
        new PointHitTestParameters(Mouse.GetPosition(element)));

      return IsMouseOverEx;
    }
  }
}
