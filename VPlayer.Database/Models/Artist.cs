using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;

namespace VPlayer.AudioStorage.Models
{
    public class Artist : BaseEntity
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
        [CanBeNull] public string MusicBrainzId { get; set; }
        [CanBeNull] public virtual List<Album> Albums { get; set; }


        /// <summary>
        /// Provide unique combination of Name
        /// </summary>
        [Index("Hash_Artist", 1, IsUnique = true)]
        [StringLength(64)]
        public override string Hash
        {
            get { return GetHashString(Name); }
            set => GetHashString(Name);
        }
    }
}
