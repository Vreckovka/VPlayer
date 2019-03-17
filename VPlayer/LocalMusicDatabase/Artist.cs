using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPlayer.Annotations;

namespace VPlayer.LocalMusicDatabase
{
    public class Artist
    {
        [Key]
        public int ArtistId { get; set; }
        [CanBeNull] public string MusicBrainzId { get; set; }
        public string Name { get; set; }
        [CanBeNull] public virtual List<Album> Albums { get; set; }
    }
}
