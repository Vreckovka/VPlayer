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
//using System.Windows.Shapes;
//using VPlayer.ViewModels.LibraryModels;

namespace VPlayer.Views.LibraryViews.LibraryPages
{
    /// <summary>
    /// Interaction logic for AlbumsPage.xaml
    /// </summary>
    public partial class AlbumsPage : Page
    {
       // private AlbumsView albumsView = new AlbumsView();
        public AlbumsPage()
        {
            InitializeComponent();
        //    DataContext = albumsView;
        }

        private void Page_Initialized(object sender, EventArgs e)
        {
          //  Task.Run(() => { albumsView.LoadAlbums(); });
        }
    }
}
