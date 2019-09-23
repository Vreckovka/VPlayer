using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VPlayer.Core.DomainClasses
{
    public class Album : INamedEntity
    {
        public Album() { }

        public int Id { get; set; }
        public string Name { get; set; }
        public virtual Artist Artist { get; set; }
        public string ReleaseDate { get; set; }
        public string MusicBrainzId { get; set; }
        public string AlbumFrontCoverURI { get; set; }
        public byte[] AlbumFrontCoverBLOB { get; set; }
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
