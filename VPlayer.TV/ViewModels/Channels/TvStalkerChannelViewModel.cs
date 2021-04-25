using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using IPTVStalker;
using Prism.Events;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPLayer.Domain.Contracts.IPTV;

namespace VPlayer.IPTV.ViewModels
{
  public class TvStalkerChannelViewModel : TvChannelViewModel
  {
    private SerialDisposable serialDisposable = new SerialDisposable();
    public readonly IPTVStalkerService stalkerService;
    private string commnad;
    public TvStalkerChannelViewModel(TvChannel model, IPTVStalkerService stalkerService, IEventAggregator eventAggregator) : base(model, eventAggregator)
    {
      this.stalkerService = stalkerService ?? throw new ArgumentNullException(nameof(stalkerService));

      Url = null;
      commnad = Model.TvItem.Source;
    }


    #region InitilizeUrl

    public override Task<string> InitilizeUrl(CancellationTokenSource cancellationToken)
    {
      if (cancellationToken == null)
      {
        cancellationToken = new CancellationTokenSource();
      }

      return Task.Run(async () =>
      {
        Url = null;

        var token = stalkerService.bearerToken;

        if (Url == null && Model != null)
        {
          Url = await stalkerService.GetLink(commnad, cancellationToken.Token);

          serialDisposable.Disposable = Observable.Timer(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10)).Subscribe(x => KeepAlive());
        }

        return Url;
      }, cancellationToken.Token);
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

      ActualCancellationTokenSource = new CancellationTokenSource();

      InitilizeUrl(ActualCancellationTokenSource);
    }

    protected override void OnSelected(bool isSelected)
    {
      base.OnSelected(isSelected);

      if (!isSelected)
      {
        serialDisposable?.Disposable?.Dispose();
      }
    }
  }
}