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
using VPlayer.AudioStorage;
using VPlayer.AudioStorage.Models;
using VPlayer.Library.ViewModels;

namespace VPlayer.Library.Views
{
    /// <summary>
    /// Interaction logic for AlbumDetailView.xaml
    /// </summary>
    public partial class AlbumDetailView : Page
    {
        public AlbumDetailViewModel AlbumDetailViewModel { get; set; }
        public AlbumDetailView(Album album)
        {
            InitializeComponent();
            AlbumDetailViewModel = new AlbumDetailViewModel(album);
            DataContext = AlbumDetailViewModel;

           
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            LibraryView.ChangeView(LibraryView.View.Albums);
        }

        private void GetMoreCovers_Click(object sender, RoutedEventArgs e)
        {
            LibraryView.ChangeView(LibraryView.View.AlbumCovers, AlbumDetailViewModel.ActualAlbum);
        }
    }
}