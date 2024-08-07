﻿using System;
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
using VPlayer.AudioStorage;
using VPlayer.Core.DomainClasses;
using VPlayer.Library.ViewModels;
using VPlayer.Library.ViewModels.AlbumsViewModels;

namespace VPlayer.Library.Views
{
    /// <summary>
    /// Interaction logic for AlbumDetailView.xaml
    /// </summary>
    public partial class AlbumDetailView : UserControl, IView
    {
        public AlbumDetailView(Album album)
        {
            InitializeComponent();
        }
    }
}
