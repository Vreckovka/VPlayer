using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
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
using PropertyChanged;
using VCore.Modularity.Interfaces;
using VPlayer.AudioInfoDownloader;
using VPlayer.AudioInfoDownloader.Models;
using VPlayer.AudioStorage;
using VPlayer.Core.DomainClasses;

namespace VPlayer.Library.Views
{
    /// <summary>
    /// Interaction logic for AvaibleCovers.xaml
    /// </summary>
    ///

    [AddINotifyPropertyChangedInterface]
    public partial class AlbumCoversView : UserControl, IView
    {
        public AlbumCoversView()
        {
            InitializeComponent();
        }

       
    }
}
