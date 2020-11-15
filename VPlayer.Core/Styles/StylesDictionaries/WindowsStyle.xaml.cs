using System.Windows;
using System.Windows.Controls;

namespace VPlayer.StylesDictionaries
{
  public partial class WindowStyle
  {
    #region Methods

    private void CloseButt_Click(object sender, System.Windows.RoutedEventArgs e)
    {
      try
      {
        Window window = (sender as Button).Tag as Window;
        window?.Close();
      }
      catch (System.Exception)
      {
      }
    }

    /// <summary>
    /// Handling event for windows button, maximize
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MaximizeButt_Click(object sender, System.Windows.RoutedEventArgs e)
    {
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