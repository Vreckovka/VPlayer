using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VPlayer.AudioStorage.Models
{
    public 
        class Genre : BaseEntity
    {
        [Key]
        public int GenreId { get; set; }
        public string Name { get; set; }

        /// <summary>
        /// Provide unique combination of Name
        /// </summary>
        [Index("Hash_Genre", 1, IsUnique = true)]
        [StringLength(64)]
        public override string Hash
        {
            get { return GetHashString(Name); }
            set => GetHashString(Name);
        }
    }
}
