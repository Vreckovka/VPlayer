﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PropertyChanged;
using VPlayer.AudioStorage.Models;
using VPlayer.Core.ViewModels;

namespace VPlayer.Library.ViewModels
{
    public class AlbumDetailViewModel : ViewModel
    {
        public Album ActualAlbum { get; set; }
        public List<Song> AlbumSongs => ActualAlbum.Songs;
        public AlbumDetailViewModel(Album album)
        {
            ActualAlbum = album;
        }
    }
}