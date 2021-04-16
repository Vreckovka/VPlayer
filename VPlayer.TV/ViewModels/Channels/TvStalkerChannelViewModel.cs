using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using IPTVStalker;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPLayer.Domain.Contracts.IPTV;

namespace VPlayer.IPTV.ViewModels
{
  public class TvStalkerChannelViewModel : TvChannelViewModel
  {
    private SerialDisposable serialDisposable = new SerialDisposable();
    public readonly IPTVStalkerService stalkerService;

    public TvStalkerChannelViewModel(TvChannel model, IPTVStalkerService stalkerService) : base(model)
    {
      this.stalkerService = stalkerService ?? throw new ArgumentNullException(nameof(stalkerService));

      Url = null;
    }


    #region InitilizeUrl

    public override Task<string> InitilizeUrl()
    {
      return Task.Run(() =>
      {
        Url = null;

        var token = stalkerService.bearerToken;
        if (Url == null && Model != null)
        {
          Url = stalkerService.GetLink(Model.Url);

          serialDisposable.Disposable = Observable.Timer(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10)).Subscribe(x => KeepAlive());
        }

        return Url;
      });
    }


    #endregion

    private void KeepAlive()
    {
      //var link = stalkerService.GetLink(Model.Url);
    }

    public void DisposeKeepAlive()
    {
      serialDisposable?.Disposable?.Dispose();
    }

    public override void RefreshSource()
    {
      stalkerService.RefreshService();

      InitilizeUrl();
    }

    protected override void OnSelected(bool isSelected)
    {
      if (!isSelected)
      {
        serialDisposable?.Disposable?.Dispose();
      }
    }
  }
}