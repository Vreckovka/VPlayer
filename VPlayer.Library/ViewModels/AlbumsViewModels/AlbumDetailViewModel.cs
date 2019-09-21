using System.Collections.Generic;
using VCore.ViewModels;
using VPlayer.Core.DomainClasses;

namespace VPlayer.Library.ViewModels.AlbumsViewModels
{
    public class AlbumDetailViewModel : ViewModel
    {
        public Album ActualAlbum { get; set; }
        public IEnumerable<Song> AlbumSongs => ActualAlbum.Songs;
        public AlbumDetailViewModel(Album album)
        {
            ActualAlbum = album;
        }
    }
}
