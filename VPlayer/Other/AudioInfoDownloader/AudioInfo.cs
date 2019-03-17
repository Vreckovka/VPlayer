using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPlayer.Other.AudioInfoDownloader
{
    public class AudioInfo
    {
        /// <summary>
        /// Audio fingerprint
        /// </summary>
        public string FingerPrint { get; set; }
        
        /// <summary>
        /// Audio duration in seconds
        /// </summary>
        public int Duration { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Title { get; set; }
        public string ArtistMBID { get; set; }

    }
}
