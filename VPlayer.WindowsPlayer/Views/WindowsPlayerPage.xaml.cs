using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using PropertyChanged;
using VPlayer.Models;
using VPlayer.ViewModels;
using VPlayer.WindowsPlayer.Models;
using VPlayer.Library;


namespace VPlayer.WindowsPlayer.Views
{

    /// <summary>
    /// Interaction logic for Player.xaml
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public partial class WindowsPlayerPage : Page
    {
        public WindowsPlayerViewModel AudioTracksViewModel { get; set; }
        private bool _isSideMenuUp;
        private DispatcherTimer _dispatcherTimer;

        public LibraryView LibraryView { get; set; }    

        public WindowsPlayerPage()
        {
            InitializeComponent();
            AudioTracksViewModel = new WindowsPlayerViewModel();
            DataContext = AudioTracksViewModel;

            _dispatcherTimer = new DispatcherTimer(DispatcherPriority.DataBind);
            _dispatcherTimer.Interval = TimeSpan.FromMilliseconds(10);
            _dispatcherTimer.Tick += _dispatcherTimer_Tick;

            PlayerHandler.Play += PlayerHandler_Play;
            PlayerHandler.Pause += PlayerHandler_Pause;
            PlayerHandler.ChangeTime += PlayerHandler_ChangeTime;


            LibraryView = new LibraryView();
            Frame_Library.Content = LibraryView;
            _isSideMenuUp = true;



            //this.Loaded += WindowsPlayerPage_Loaded;

            //DatabaseManager.UpdateAlbumsConversBLOB();

            //Task.Run(() => { AudioTracksViewModel.AddFiles(new string[] { "D:\\Hudba\\Disturbed Discography" }); });
            //Task.Run(() => { AudioTracksViewModel.UpdateDatabaseFromFolder("D:\\Hudba\\Disturbed Discography"); });
        }

        private void WindowsPlayerPage_Loaded(object sender, RoutedEventArgs e)
        {
            MenuItem_Click(null, null);
        }

        private void _dispatcherTimer_Tick(object sender, EventArgs e)
        {
            AudioTracksViewModel.ActualTime = MediaElement_Player.Position;
        }

        private void PlayerHandler_ChangeTime(object sender, double e)
        {
            MediaElement_Player.Position = TimeSpan.FromMilliseconds(e);
            AudioTracksViewModel.ManualChanged = false;
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
            if (sender != AudioTracksViewModel && IsLoaded)
            {
                Play();
            }
        }
        private void LoadFiles_Click(object sender, RoutedEventArgs e)
        {
            //System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();
            //var result = dlg.ShowDialog();
            //Console.WriteLine(result);
        }
        private void ListView_AudioTracks_Drop(object sender, System.Windows.DragEventArgs e)
        {
            //if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            //{
            //    string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);

            //    Task.Run(() => { AudioTracksViewModel.AddFiles(files); });
            //}
        }
        private void Pause()
        {
            AudioTracksViewModel.IsPlaying = false;

            MediaElement_Player.Pause();
            PlayerHandler.OnPause(this);
        }
        private void Play(Uri uri = null, bool next = false)
        {
            if (uri == null)
            {
                if (AudioTracksViewModel.ActualTrack.Uri != null)
                {
                    //AudioTracksViewModel.Play(uri, next);
                    //MediaElement_Player.Source = AudioTracksViewModel.ActualTrack.Uri;

                    //_dispatcherTimer.Stop();
                    //_dispatcherTimer.Start();
                }
            }
            else
            {
                //AudioTracksViewModel.Play(uri, next);
                //MediaElement_Player.Source = uri;
            }

            MediaElement_Player.Play();
        }
        private void ListViewItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            Play(((AudioTrack)((ListViewItem)sender).Content).Uri);
        }

        private void ListView_Tracks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var audioTrack = (AudioTrack)((ListView)sender).SelectedItem;

            //if (audioTrack.AlbumImageURL == null)
            //{
            //    Task.Run(() => { AudioTracksViewModel.AudioInfoDownloader.SetAlbumFrontCover(ref audioTrack); });
            //}
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            TimeSpan timeSpan = new TimeSpan(0, 0, 0, 0, 350);
            Frame_Library.HorizontalAlignment = HorizontalAlignment.Left;
            Frame_Library.Opacity = 1;


            if (!_isSideMenuUp)
            {
                Frame_Library.Margin = new Thickness(0);

                if (Application.Current.MainWindow != null)
                {
                    Frame_Library.Width = Application.Current.MainWindow.ActualWidth;
                    ThicknessAnimation doubleAnimationWidth = new ThicknessAnimation(new Thickness(-1200, 0, 0, 0),new Thickness(0), timeSpan);
                   // DoubleAnimation doubleAnimationWidth = new DoubleAnimation(0, Application.Current.MainWindow.ActualWidth, timeSpan);

                    doubleAnimationWidth.Completed += DoubleAnimationWidth_Completed;
                    Frame_Library.BeginAnimation(FrameworkElement.MarginProperty, doubleAnimationWidth);
                }

                _isSideMenuUp = true;
            }
            else
            {
                Frame_Library.Width = Frame_Library.ActualWidth;
                ThicknessAnimation doubleAnimationWidth = new ThicknessAnimation(new Thickness(0), new Thickness(-1200, 0, 0, 0), timeSpan);

                doubleAnimationWidth.Completed += DoubleAnimationWidth_Completed1;
                Frame_Library.BeginAnimation(FrameworkElement.MarginProperty, doubleAnimationWidth);
                _isSideMenuUp = false;
                ListView_SideMenu.UnselectAll();
                ListView_SideMenu.Items.Refresh();
            }
        }

        private void DoubleAnimationWidth_Completed1(object sender, EventArgs e)
        {
            Frame_Library.BeginAnimation(MarginProperty, null);
            Frame_Library.Width = 0;
        }

        private void DoubleAnimationWidth_Completed(object sender, EventArgs e)
        {
            Frame_Library.BeginAnimation(WidthProperty, null);
          
            Frame_Library.HorizontalAlignment = HorizontalAlignment.Stretch;
            Frame_Library.Width = double.NaN;
        }

        private void SongEnd(object sender, RoutedEventArgs e)
        {
            Play(null, true);
        }
    }
}
