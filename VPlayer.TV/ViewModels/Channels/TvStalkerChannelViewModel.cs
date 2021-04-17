using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
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

    public override Task<string> InitilizeUrl(CancellationToken cancellationToken)
    {
      return Task.Run(async () =>
      {
        Url = null;

        var token = stalkerService.bearerToken;
        if (Url == null && Model != null)
        {
          Url = await stalkerService.GetLink(Model.TvItem.Source,cancellationToken);

          serialDisposable.Disposable = Observable.Timer(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10)).Subscribe(x => KeepAlive());
        }

        return Url;
      }, cancellationToken);
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

      InitilizeUrl(new CancellationTokenSource().Token);
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