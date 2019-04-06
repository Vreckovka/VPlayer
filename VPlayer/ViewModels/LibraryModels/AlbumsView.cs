using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPlayer.LocalMusicDatabase;
using VPlayer.Models.LibraryModels;

namespace VPlayer.ViewModels.LibraryModels
{
    class AlbumsView : BaseLibraryModel
    {
        public List<AlbumInfo> Albums { get; set; }
        private LocalMusicDbContext _localMusicDbContext;
        public AlbumsView()
        {
            _localMusicDbContext = new LocalMusicDbContext();
        }

        public void LoadAlbums(string interpert = null)
        {
            if (interpert == null)
            {
                Albums = (from x in _localMusicDbContext.Albums
                    join y in _localMusicDbContext.Artists on x.Artist equals y
                    orderby y.Name,x.ReleaseDate
                    select new AlbumInfo()
                    {
                        Album = x,
                        Artist = y
                    }).ToList();

            }
        }
    }
}
