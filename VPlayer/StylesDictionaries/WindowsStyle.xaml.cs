using System.Windows;
using System.Windows.Controls;

namespace VPlayer.StylesDictionaries
{
  public partial class WindowStyle
  {
    #region Methods

    private void CloseButt_Click(object sender, System.Windows.RoutedEventArgs e)
    {
      Window window = (sender as Button).Tag as Window;
      window.Close();
    }

    /// <summary>
    /// Handling event for windows button, maximize
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MaximizeButt_Click(object sender, System.Windows.RoutedEventArgs e)
    {
      //public bool IsFullscreen
      //{
      //  get
      //  {
      //    return WindowState == System.Windows.WindowState.Maximized
      //           && ResizeMode == System.Windows.ResizeMode.NoResize
      //           && WindowStyle == System.Windows.WindowStyle.None;
      //  }
      //  set
      //  {
      //    if (value)
      //    {
      //      ResizeMode = System.Windows.ResizeMode.NoResize;
      //      WindowStyle = System.Windows.WindowStyle.None;
      //      WindowState = System.Windows.WindowState.Maximized;
      //    }
      //    else
      //    {
      //      ResizeMode = System.Windows.ResizeMode.CanResize;
      //      WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
      //      WindowState = System.Windows.WindowState.Normal;
      //    }
      //  }

      Window window = (sender as Button).Tag as Window;

      if (window.WindowState != WindowState.Maximized)
      {
        window.WindowState = WindowState.Maximized;
      }
      else
      {
        window.WindowState = WindowState.Normal;
      }
    }

    /// <summary>
    /// Handling event for windows button , minimize
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MinimizeButt_Click(object sender, System.Windows.RoutedEventArgs e)
    {
      Window window = (sender as Button).Tag as Window;
      window.WindowState = System.Windows.WindowState.Minimized;
    }

    #endregion Methods
  }
}