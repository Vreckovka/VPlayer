using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VPlayer.AudioStorage.Models
{
    public 
        class Genre 
    {
        public Genre()
        {
            
        }

        [Key]
        public int GenreId { get; set; }
        public string Name { get; set; }

    }
}
