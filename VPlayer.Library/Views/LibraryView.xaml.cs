using System;
using System.Windows.Controls;
using Prism.Events;
using VPlayer.AudioStorage.Models;
using VPlayer.Library.ViewModels;

namespace VPlayer.Library.Views
{
    /// <summary>
    /// Interaction logic for LibraryView.xaml
    /// </summary>
    public partial class LibraryView : UserControl
    {
        private static AlbumsView AlbumsView = new AlbumsView();
        private static ArtistsView ArtistsView;


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
        }

        public static void ChangeView(View view, Album album = null, Artist artist = null)
        {
            //switch (view)
            //{
            //    case View.Albums:
            //        _frame.Content = AlbumsView;
            //        break;
            //    case View.AlbumDetail:
            //        _frame.Content = new AlbumDetailView(album);
            //        break;
            //    case View.AlbumCovers:
            //        _frame.Content = new AlbumCoversView(album);
            //        break;
            //    case View.Artists:
            //        _frame.Content = ArtistsView;
            //        break;
            //    case View.ArtistDetail:
            //        _frame.Content = new ArtistDetailView(artist);
            //        break;
            //    default:
            //        throw new ArgumentOutOfRangeException(nameof(view), view, null);
            //}
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

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
           var asd = ((LibraryViewModel) DataContext).ActualCollectionViewModel.PlayableItems[150];
        }
    }
}
