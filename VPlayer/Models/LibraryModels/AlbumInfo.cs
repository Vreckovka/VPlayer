using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PropertyChanged;
using VPlayer.LocalMusicDatabase;

namespace VPlayer.Models.LibraryModels
{
    [AddINotifyPropertyChangedInterface]
    class AlbumInfo
    {
        public Album Album { get; set; }
        public Artist Artist { get; set; }
    }
}
