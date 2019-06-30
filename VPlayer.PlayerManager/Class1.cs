using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPlayer.AudioStorage.Models;

namespace VPlayer.PlayerManager
{
    public class PlayerManager
    {
        public IEnumerable<BaseEntity> PlayList { get; set; }
        public BaseEntity ActualEntity { get; set; }


        public void Play()
        {

        }

        public void Stop()
        {

        }

        public void Pause()
        {

        }
    }
}
