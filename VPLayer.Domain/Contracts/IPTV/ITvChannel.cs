using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace VPLayer.Domain.Contracts.IPTV
{
  public interface ITvChannel
  {
    public CancellationTokenSource ActualCancellationTokenSource { get; }
    Task<string> InitilizeUrl(CancellationTokenSource cancellationTokenSource = null);
    public void RefreshSource();
    public string Url { get; set; }
    public bool IsSelectedToPlay { get; set; }
    public bool IsSelected { get; set; }
  }
}