using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VPlayer.Core.DomainClasses
{
    public class Artist : INamedEntity
    {
        public Artist()
        { }
        public Artist(string name)
        {
            Name = name;
        }

        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public byte[] ArtistCover { get; set; }

        /// <summary>
        /// Album id  wich is used as cover for Artist
        /// </summary>
        public int? AlbumIdCover { get; set; }
        public string MusicBrainzId { get; set; }
        public virtual ICollection<Album> Albums { get; set; }
    }
}
