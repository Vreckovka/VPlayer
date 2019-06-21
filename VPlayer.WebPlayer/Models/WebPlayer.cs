using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PropertyChanged;

namespace VPlayer.Models
{
    [AddINotifyPropertyChangedInterface]
    class InternetPlayer
    {
        public Uri Uri { get; set; }
        public string Title { get; set; }
        public string HTMLElementClass { get; set; }
    }
}
