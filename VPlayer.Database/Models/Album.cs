using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;
using PropertyChanged;
using VPlayer.AudioStorage.Interfaces;

namespace VPlayer.AudioStorage.Models
{
    [AddINotifyPropertyChangedInterface]
    public class Album : BaseEntity
    {
        public Album() { }
        public Album(string name, Artist artist)
        {
            Name = name;
            Artist = artist;
        }


        [Key]
        public int AlbumId { get; set; }
        public string Name { get; set; }
        public virtual Artist Artist { get; set; }
        [CanBeNull] public string ReleaseDate { get; set; }
        [CanBeNull] public string MusicBrainzId { get; set; }
        [CanBeNull] public string AlbumFrontCoverURI { get; set; }
        [CanBeNull] public byte[] AlbumFrontCoverBLOB { get; set; }


        public virtual List<Song> Songs { get; set; }

        /// <summary>
        /// Provide unique combination of Name, Album
        /// </summary>
        [Index("Hash_Album",IsUnique = true)]
        [StringLength(64)]
        public override string Hash
        {
            get
            {
                return GetHashString(Name + Artist?.Name);
            }
            set => GetHashString(Name + Artist?.Name + ReleaseDate);
        }

        public void UpdateAlbum(Album album)
        {
            Name = album.Name;
            AlbumFrontCoverBLOB = album.AlbumFrontCoverBLOB;
            AlbumFrontCoverURI = album.AlbumFrontCoverURI;
            MusicBrainzId = album.MusicBrainzId;
            ReleaseDate = album.ReleaseDate;
        }
    }
}
