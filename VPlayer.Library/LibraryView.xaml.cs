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
using VPlayer.Library.ViewModels;

namespace VPlayer.Library
{
    /// <summary>
    /// Interaction logic for LibraryView.xaml
    /// </summary>
    public partial class LibraryView : UserControl
    {
        public LibraryView()
        {
            InitializeComponent();
            Frame_LibraryContent.Source = new Uri("Views/AlbumsView.xaml", UriKind.Relative);
            LibraryViewModel.Init();
        }

        private void ListViewItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (((ListViewItem)sender).Content.Equals("Albums"))
            {
                Frame_LibraryContent.Source = new Uri("Views/AlbumsView.xaml", UriKind.Relative);
            }
        }

        private void Page_Initialized(object sender, EventArgs e)
        {

        }
    }
}
