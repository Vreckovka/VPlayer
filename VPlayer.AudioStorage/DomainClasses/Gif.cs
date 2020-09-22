using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPlayer.AudioStorage.DomainClasses
{
  public class Gif : DomainEntity
  {
    public string Url { get; set; }
    public string GiphyId { get; set; }
  }
}
