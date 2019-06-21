using System;
using PropertyChanged;

//using VPlayer.Annotations;

namespace VPlayer.WindowsPlayer.Models
{
    [AddINotifyPropertyChangedInterface]
    public class AudioTrack 
    {
        public string Name { get; set; }
        public TimeSpan Duration { get; set; }
        public string Artist { get; set; }
        public Uri Uri { get; set; }

        public override string ToString()
        {
            return $"{Name} {Duration} {Artist} {Uri}";
        }

    }
}
