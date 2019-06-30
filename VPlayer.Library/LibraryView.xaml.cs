using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VPlayer.AudioStorage.Models;
using VPlayer.Library.ViewModels;
using VPlayer.Library.Views;

namespace VPlayer.Library
{
    /// <summary>
    /// Interaction logic for LibraryView.xaml
    /// </summary>
    public partial class LibraryView : Page
    {
        private static AlbumsView AlbumsView = new AlbumsView();
        private static ArtistsView ArtistsView = new ArtistsView();

        public static Frame _frame { get; private set; }

        public enum View
        {
            Albums,
            AlbumDetail,
            AlbumCovers,

            Artists,
            ArtistDetail,

        }

        public LibraryView()
        {
            InitializeComponent();
            _frame = Frame_LibraryContent;

            Frame_LibraryContent.Content = ArtistsView;
        }

        public static void ChangeView(View view, Album album = null, Artist artist = null)
        {
            switch (view)
            {
                case View.Albums:
                    _frame.Content = AlbumsView;
                    break;
                case View.AlbumDetail:
                    _frame.Content = new AlbumDetailView(album);
                    break;
                case View.AlbumCovers:
                    _frame.Content = new AlbumCoversView(album);
                    break;
                case View.Artists:
                    _frame.Content = ArtistsView;
                    break;
                case View.ArtistDetail:
                    _frame.Content = new ArtistDetailView(artist);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(view), view, null);
            }
        }

        private void MenuListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = ((ListViewItem)e.AddedItems[0]).Content;

            if (IsLoaded)
                switch (selectedItem)
                {
                    case "Albums":
                        ChangeView(View.Albums);
                        break;
                    case "Artist":
                        ChangeView(View.Artists);
                        break;
                }
        }
    }
}
