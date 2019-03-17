using System.Windows;
using System.Windows.Controls;

namespace MultiMusicPlayer.StylesDictionaries
{
    public partial class WindowStyle
    {
        private void CloseButt_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Window window = (sender as Button).Tag as Window;
            window.Close();
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

        /// <summary>
        /// Handling event for windows button, maximize
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MaximizeButt_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Window window = (sender as Button).Tag as Window;

            if (window.WindowState != System.Windows.WindowState.Maximized)
            {
                window.WindowState = System.Windows.WindowState.Maximized;
            }
            else
            {
                window.WindowState = System.Windows.WindowState.Normal;
            }
        }
    }
}
