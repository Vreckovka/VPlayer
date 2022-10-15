using System.Collections.Generic;
using System.Threading.Tasks;
using LibVLCSharp.Shared;

namespace VVLC.Providers
{
  public interface IVlcProvider
  {
    KeyValuePair<MediaPlayer, LibVLC> InitlizeVlc();
    LibVLC GetLibVLC();
  }
}