using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MultiMusicPlayer.Annotations;
using MultiMusicPlayer.LocalDatabase;

namespace MultiMusicPlayer.LocalMusicDatabase
{
    public class Album
    {
        [Key]
        public int AlbumId { get; set; }
        public string Name { get; set; }
        public Artist Artist { get; set; }
        public string ReleaseDate { get; set; }
        [CanBeNull] public string AlbumFrontCoverURI { get; set; }
        public virtual List<Song> Songs { get; set; }
    }
}
