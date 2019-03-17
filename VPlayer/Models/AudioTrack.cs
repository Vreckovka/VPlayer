using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using MultiMusicPlayer.Annotations;
using PropertyChanged;

namespace MultiMusicPlayer.Models
{
    [AddINotifyPropertyChangedInterface]
    public class AudioTrack 
    {
        public string Name { get; set; }
        public TimeSpan Duration { get; set; }
        [CanBeNull] public string Artist { get; set; }
        public Uri Uri { get; set; }
        public bool IsPlaying { get; set; }
       
    }
}
