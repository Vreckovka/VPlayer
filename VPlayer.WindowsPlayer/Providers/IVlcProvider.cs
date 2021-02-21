using System.Threading.Tasks;
using Vlc.DotNet.Wpf;

namespace VPlayer.WindowsPlayer.Providers
{
  public interface IVlcProvider
  {
    Task InitlizeVlc(VlcControl vlcControl);
  }
}