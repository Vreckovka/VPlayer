using System.Windows.Controls;
using VCore.Standard.Modularity.Interfaces;
using VPlayer.Library.Views;

namespace VPlayer.WindowsPlayer.Views
{
  /// <summary>
  /// Interaction logic for Player.xaml
  /// </summary>
  public partial class WindowsView : UserControl, IView
  {
    #region Fields

    private bool _isSideMenuUp;

    #endregion Fields

    #region Constructors

    public WindowsView()
    {
      InitializeComponent();

      //PlayerHandler.Play += PlayerHandler_Play;
      //PlayerHandler.Pause += PlayerHandler_Pause;
      //PlayerHandler.ChangeTime += PlayerHandler_ChangeTime;

      // LibraryView = new LibraryView(eventAggregator);
      _isSideMenuUp = false;

      //this.Loaded += WindowsPlayerPage_Loaded;

      //DatabaseManager.UpdateAlbumsConversBLOB();

      //Task.Run(() => { AudioTracksViewModel.AddFiles(new string[] { "D:\\Hudba\\Disturbed Discography" }); });
      //Task.Run(() => { AudioTracksViewModel.UpdateDatabaseFromFolder("D:\\Hudba\\Disturbed Discography"); });
    }

    #endregion Constructors

    #region Properties

    public LibraryView LibraryView { get; set; }

    #endregion Properties

    //private void WindowsPlayerPage_Loaded(object sender, RoutedEventArgs e)
    //{
    //    // MenuItem_Click(null, null);
    //}

    //private void PlayerHandler_ChangeTime(object sender, double e)
    //{
    //}

    //private void PlayerHandler_Pause(object sender, EventArgs e)
    //{
    //    if (sender != this && IsLoaded)
    //    {
    //        Pause();
    //    }
    //}
    //private void PlayerHandler_Play(object sender, EventArgs e)
    //{
    //}
    //private void LoadFiles_Click(object sender, RoutedEventArgs e)
    //{
    //    //System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();
    //    //var result = dlg.ShowDialog();
    //    //Console.WriteLine(result);
    //}
    //private void ListView_AudioTracks_Drop(object sender, System.Windows.DragEventArgs e)
    //{
    //    //if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
    //    //{
    //    //    string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);

    //    //    Task.Run(() => { AudioTracksViewModel.AddFiles(files); });
    //    //}
    //}
    //private void Pause()
    //{
    //    //PlayerHandler.OnPause(this);
    //}
    //private void Play(Uri uri = null, bool next = false)
    //{
    //    //if (uri == null)
    //    //{
    //    //    if (AudioTracksViewModel.ActualTrack.Uri != null)
    //    //    {
    //    //        //AudioTracksViewModel.Play(uri, next);
    //    //        //MediaElement_Player.Source = AudioTracksViewModel.ActualTrack.Uri;

    //    //        //_dispatcherTimer.Stop();
    //    //        //_dispatcherTimer.Start();
    //    //    }
    //    //}
    //    //else
    //    //{
    //    //    //AudioTracksViewModel.Play(uri, next);
    //    //    //MediaElement_Player.Source = uri;
    //    //}

    //}
    //private void ListViewItem_DoubleClick(object sender, MouseButtonEventArgs e)
    //{
    //    Play(((AudioTrack)((ListViewItem)sender).Content).Uri);
    //}

    //private void ListView_Tracks_SelectionChanged(object sender, SelectionChangedEventArgs e)
    //{
    //    var audioTrack = (AudioTrack)((ListView)sender).SelectedItem;

    //    //if (audioTrack.AlbumImageURL == null)
    //    //{
    //    //    Task.Run(() => { AudioTracksViewModel.AudioInfoDownloader.SetAlbumFrontCover(ref audioTrack); });
    //    //}
    //}

    //private void MenuItem_Click(object sender, RoutedEventArgs e)
    //{
    //    TimeSpan timeSpan = new TimeSpan(0, 0, 0, 0, 350);
    //    LibraryRegion.HorizontalAlignment = HorizontalAlignment.Left;
    //    LibraryRegion.Opacity = 1;

    //    if (Application.Current.MainWindow != null)
    //    {
    //        if (!_isSideMenuUp)
    //        {
    //            LibraryRegion.Margin = new Thickness(0);

    //            LibraryRegion.Width = Application.Current.MainWindow.ActualWidth;
    //            ThicknessAnimation doubleAnimationWidth = new ThicknessAnimation(new Thickness(-Application.Current.MainWindow.ActualWidth, 0, 0, 0), new Thickness(0), timeSpan);

    //            doubleAnimationWidth.Completed += DoubleAnimationWidth_Completed;
    //            LibraryRegion.BeginAnimation(FrameworkElement.MarginProperty, doubleAnimationWidth);

    //            _isSideMenuUp = true;
    //        }
    //        else
    //        {
    //            LibraryRegion.Width = LibraryRegion.ActualWidth;

    //            ThicknessAnimation doubleAnimationWidth = new ThicknessAnimation(new Thickness(0), new Thickness(-Application.Current.MainWindow.ActualWidth, 0, 0, 0), timeSpan);

    //            doubleAnimationWidth.Completed += DoubleAnimationWidth_Completed1;
    //            LibraryRegion.BeginAnimation(FrameworkElement.MarginProperty, doubleAnimationWidth);

    //            _isSideMenuUp = false;
    //        }
    //    }
    //}

    //private void DoubleAnimationWidth_Completed1(object sender, EventArgs e)
    //{
    //    LibraryRegion.BeginAnimation(MarginProperty, null);
    //    LibraryRegion.Width = 0;
    //}

    //private void DoubleAnimationWidth_Completed(object sender, EventArgs e)
    //{
    //    LibraryRegion.BeginAnimation(WidthProperty, null);

    //    LibraryRegion.HorizontalAlignment = HorizontalAlignment.Stretch;
    //    LibraryRegion.Width = double.NaN;
    //}

    //private void SongEnd(object sender, RoutedEventArgs e)
    //{
    //    Play(null, true);
    //}
  }
}