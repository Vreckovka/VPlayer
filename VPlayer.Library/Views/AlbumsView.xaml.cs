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
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PropertyChanged;
using VPlayer.AudioStorage.Models;
using VPlayer.Library.ViewModels;

namespace VPlayer.Library.Views
{
    /// <summary>
    /// Interaction logic for AlbumsView.xaml
    /// </summary>


    public partial class AlbumsView : Page
    {
        public AlbumsView()
        {
            InitializeComponent();
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LibraryView.ChangeView(LibraryView.View.AlbumDetail, (Album)Albums.SelectedItem);
        }
    }
}
