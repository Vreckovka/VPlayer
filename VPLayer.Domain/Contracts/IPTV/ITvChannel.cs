using System.ComponentModel;
using System.Threading.Tasks;

namespace VPLayer.Domain.Contracts.IPTV
{
  public interface ITvChannel
  {
    Task<string> InitilizeUrl();
    public void RefreshSource();
    public string Url { get; set; }
    public bool IsSelectedToPlay { get; set; }
    public bool IsSelected { get; set; }
  }
}