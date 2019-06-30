using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PropertyChanged;

namespace VPlayer.AudioStorage.Models
{
    [AddINotifyPropertyChangedInterface]
    public class BaseEntity
    {
        [NotMapped]
        public bool IsPlaying { get; set; }
    }
}
