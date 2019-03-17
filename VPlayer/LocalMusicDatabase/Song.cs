using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiMusicPlayer.LocalMusicDatabase
{
   public class Song
    {
        [Key]
        public int SongId { get; set; }
        public Album Album { get; set; }
        public string DiskLocation { get; set; }
        public string Name { get; set; }
        public int Length { get; set; }
    }
}
