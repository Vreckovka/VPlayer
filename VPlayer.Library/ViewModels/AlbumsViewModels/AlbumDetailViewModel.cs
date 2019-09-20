using System.Collections.Generic;
using VCore.ViewModels;
using VPlayer.AudioStorage.Models;

namespace VPlayer.Library.ViewModels.AlbumsViewModels
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
