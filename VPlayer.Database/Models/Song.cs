using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;
using JetBrains.Annotations;

namespace VPlayer.AudioStorage.Models
{
    public class Song : BaseEntity
    {
        public Song() { }
        public Song(string name, Album album, Artist artist)
        {
            Name = name;
            Album = album;
            Artist = artist;
        }


        [Key]
        public int SongId { get; set; }
        public string Name { get; set; }
        public string DiskLocation { get; set; }
        public int Length { get; set; }
        [CanBeNull] public string MusicBrainzId { get; set; }
        public virtual Album Album { get; set; }
        [CanBeNull] public virtual Genre Genre { get; set; }
        public virtual Artist Artist { get; set; }

        /// <summary>
        /// Provide unique combination of Name, Album, Artist
        /// </summary>
        [Index("Hash_Song", 1, IsUnique = true)]
        [StringLength(64)]
        public override string Hash
        {
            get { return GetHashString(Name + Album?.Name + Artist?.Name); }
            set => GetHashString(Name + Album?.Name + Artist?.Name);
        }
    }
}
