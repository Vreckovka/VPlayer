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
using VCore.Modularity.Interfaces;
using VPlayer.Core.DomainClasses;
using VPlayer.Library.ViewModels.LibraryViewModels.ArtistsViewModels;

namespace VPlayer.Library.Views
{
    /// <summary>
    /// Interaction logic for ArtistDetail.xaml
    /// </summary>
    public partial class ArtistDetailView : UserControl, IView
    {
        public ArtistDetailView()
        {
            InitializeComponent();
        }
    }
}
