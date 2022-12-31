using System.Collections.Generic;
using System.Threading.Tasks;
using LibVLCSharp.Shared;

namespace VVLC.Providers
{
  public interface IVlcProvider
  {
    LibVLC InitlizeVlc();
    LibVLC GetLibVLC();
  }
}