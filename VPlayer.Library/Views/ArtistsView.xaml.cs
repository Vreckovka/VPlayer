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
using Prism.Events;
using VCore.Modularity.Interfaces;
using VPlayer.Library.ViewModels;
using VPlayer.Library.ViewModels.LibraryViewModels.ArtistsViewModels;
using WpfToolkit.Controls;

namespace VPlayer.Library.Views
{
    /// <summary>
    /// Interaction logic for ArtistsView.xaml
    /// </summary>
    public partial class ArtistsView : UserControl , IView
    {
        public ArtistsView()
        {
            InitializeComponent();
        }
    }
}
