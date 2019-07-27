using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;

namespace VPlayer.AudioStorage.Models
{
    public class Artist 
    {
        public Artist() 
        { }
        public Artist(string name)
        {
           Name = name; 
        }

        [Key]
        public int ArtistId { get; set; }
        public string Name { get; set; }
        [CanBeNull] public byte[] ArtistCover { get; set; }

        /// <summary>
        /// Album id  wich is used as cover for Artist
        /// </summary>
        [CanBeNull] public int? AlbumIdCover { get; set; }
        [CanBeNull] public string MusicBrainzId { get; set; }
        [CanBeNull] public virtual ICollection<Album> Albums { get; set; }
    }
}
