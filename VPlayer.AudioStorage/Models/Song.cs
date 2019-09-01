using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;
using JetBrains.Annotations;
using VPlayer.AudioStorage.Interfaces;

namespace VPlayer.AudioStorage.Models
{
    public class Song : INamedEntity
    {
        public Song() { }
        public Song(string name, Album album)
        {
            Name = name;
            Album = album;
        }


        [Key]
        public int SongId { get; set; }
        public string Name { get; set; }
        public string DiskLocation { get; set; }
        public int Length { get; set; }

        [CanBeNull] public string MusicBrainzId { get; set; }
        public virtual Album Album { get; set; }

        public override string ToString()
        {
            return $"{Name}|{Album}";
        }
    }
}

