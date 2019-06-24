using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;
using PropertyChanged;
using VPlayer.AudioStorage.Interfaces;

namespace VPlayer.AudioStorage.Models
{
    [AddINotifyPropertyChangedInterface]
    public class Album 
    {
        public Album() { }

        [Key]
        public int AlbumId { get; set; }
        public string Name { get; set; }
        public virtual Artist Artist { get; set; }
        [CanBeNull] public string ReleaseDate { get; set; }
        [CanBeNull] public string MusicBrainzId { get; set; }
        [CanBeNull] public string AlbumFrontCoverURI { get; set; }
        [CanBeNull] public byte[] AlbumFrontCoverBLOB { get; set; }


        public virtual List<Song> Songs { get; set; }
        public void UpdateAlbum(Album album)
        {
            Name = album.Name;
            AlbumFrontCoverBLOB = album.AlbumFrontCoverBLOB;
            AlbumFrontCoverURI = album.AlbumFrontCoverURI;
            MusicBrainzId = album.MusicBrainzId;
            ReleaseDate = album.ReleaseDate;
        }

        public override string ToString()
        {
            return $"{Name}|{Artist}";
        }
    }
}
