using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Listener;
using VPlayer.Models;
using VPlayer.ViewModels;

namespace VPlayer.Views
{

    /// <summary>
    /// Interaction logic for Player.xaml
    /// </summary>
    public partial class WindowsPlayerPage : Page
    {
        public WindowsPlayerView AudioTracksView { get; set; }
        private bool _isSideMenuUp;
        public WindowsPlayerPage()
        {
            InitializeComponent();
            AudioTracksView = new WindowsPlayerView();
            DataContext = AudioTracksView;

            PlayerHandler.Play += PlayerHandler_Play;
            PlayerHandler.Pause += PlayerHandler_Pause;
            PlayerHandler.ChangeTime += PlayerHandler_ChangeTime;

            Task.Run(() => { AudioTracksView.AddFiles(new string[] { "D:\\Hudba\\Disturbed Discography" }); });
            Task.Run(() => { AudioTracksView.UpdateDatabaseFromFolder("C:\\Users\\Roman Pecho\\Desktop\\Skuska"); });
        }

        private void PlayerHandler_ChangeTime(object sender, double e)
        {
            MediaElement_Player.Position = TimeSpan.FromMilliseconds(e);
        }

        private void PlayerHandler_Pause(object sender, EventArgs e)
        {
            if (sender != this && IsLoaded)
            {
                Pause();
            }
        }
        private void PlayerHandler_Play(object sender, EventArgs e)
        {
            if (sender != AudioTracksView && IsLoaded)
            {
                Play();
            }
        }
        private void LoadFiles_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();
            var result = dlg.ShowDialog();
            Console.WriteLine(result);
        }
        private void ListView_AudioTracks_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);

                Task.Run(() => { AudioTracksView.AddFiles(files); });
            }
        }
        private void Pause()
        {
            AudioTracksView.IsPlaying = false;

            MediaElement_Player.Pause();
            PlayerHandler.OnPause(this);
        }
        private void Play(Uri uri = null)
        {
            if (uri == null)
            {
                MediaElement_Player.Source = AudioTracksView.ActualTrack.Uri;
                AudioTracksView.Play();
            }
            else
            {
                AudioTracksView.Play(uri);
                MediaElement_Player.Source = uri;
            }

            MediaElement_Player.Play();
        }
        private void ListViewItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            Play(((AudioTrack)((ListViewItem)sender).Content).Uri);
        }

        private void Slider_Time_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            double valueInMilli = (Slider_Time.Value * (AudioTracksView.ActualTrack.Duration).TotalMilliseconds) / 100;
            PlayerHandler.OnChangeTime(valueInMilli); ;
        }

        private void ListView_Tracks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var audioTrack = (AudioTrack)((ListView)sender).SelectedItem;

            //if (audioTrack.AlbumImageURL == null)
            //{
            //    Task.Run(() => { AudioTracksView.AudioInfoDownloader.SetAlbumFrontCover(ref audioTrack); });
            //}
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            TimeSpan timeSpan = new TimeSpan(0, 0, 0, 0, 350);
            Frame_Library.HorizontalAlignment = HorizontalAlignment.Left;

            if (!_isSideMenuUp)
            {
                DoubleAnimation doubleAnimationWidth = new DoubleAnimation(Frame_Library.ActualWidth,
                    Application.Current.MainWindow.ActualWidth, timeSpan);

                doubleAnimationWidth.Completed += DoubleAnimationWidth_Completed;
                Frame_Library.BeginAnimation(FrameworkElement.WidthProperty, doubleAnimationWidth);

                _isSideMenuUp = true;
            }
            else
            {
                DoubleAnimation doubleAnimationWidth = new DoubleAnimation(Frame_Library.ActualWidth, 0, timeSpan);

                Frame_Library.BeginAnimation(FrameworkElement.WidthProperty, doubleAnimationWidth);
                _isSideMenuUp = false;
                ListView_SideMenu.UnselectAll();
                ListView_SideMenu.Items.Refresh();
            }
        }

        private void DoubleAnimationWidth_Completed(object sender, EventArgs e)
        {
            Frame_Library.BeginAnimation(WidthProperty, null);
            Frame_Library.HorizontalAlignment = HorizontalAlignment.Stretch;
            Frame_Library.Width = double.NaN;
        }
    }

    public class ActualTimeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[1] != DependencyProperty.UnsetValue)
            {
                //If value changed based on Time
                if (values[0] is TimeSpan)
                {
                    return (100 * ((TimeSpan)values[0]).TotalMilliseconds) / ((TimeSpan)values[1]).TotalMilliseconds;
                }
                else
                {
                    //if value changed based on slider value
                    double valueInMilli = ((double)values[0] * ((TimeSpan)values[1]).TotalMilliseconds) / 100;

                    WindowsPlayerView.OnActualTimeChanged(valueInMilli);


                    return TimeSpan.FromMilliseconds(valueInMilli).ToString("hh\\:mm\\:ss");
                }
            }
            else
                return 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            Console.WriteLine(value);
            return null;
        }
    }


}
