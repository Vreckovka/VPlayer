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
using WpfToolkit.Controls;

namespace VPlayer.Library.Views
{
    /// <summary>
    /// Interaction logic for ArtistsView.xaml
    /// </summary>
    public partial class ArtistsView : Page
    {
        private ArtistsViewModel _artistsViewModel;
        public ArtistsView()
        {
            InitializeComponent();
            _artistsViewModel = new ArtistsViewModel();
            DataContext = _artistsViewModel;
          
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((Artist) Artists.SelectedItem != null)
            {
                LibraryView.ChangeView(LibraryView.View.ArtistDetail, artist: (Artist) Artists.SelectedItem);
                Artists.UnselectAll();
            }
        }

        private void TextBox_Finder_TextChanged(object sender, TextChangedEventArgs e)
        {
            _artistsViewModel.SetArtistsByName(((TextBox)e.Source).Text.ToLower());
        }
    }
}
