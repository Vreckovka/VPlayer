using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Prism.Events;
using VCore.Standard;
using VCore.Standard.ViewModels.TreeView;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPLayer.Domain.Contracts.IPTV;
using VPlayer.IPTV.Events;

namespace VPlayer.IPTV.ViewModels
{
  public class TvChannelViewModel : TvChannelViewModel<TvChannel>, ITvPlayableItem, ITvChannel
  {
    private readonly IEventAggregator eventAggregator;

    public TvChannelViewModel(TvChannel model, IEventAggregator eventAggregator) : base(model)
    {
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
      Url = model.TvItem.Source;
      Name = model.Name;
    }

    public virtual Task<string> InitilizeUrl(CancellationTokenSource cancellationToken)
    {
      return Task.Run(() => { return Model.TvItem.Source; });
  }


    public ITvChannel SelectedTvChannel
    {
      get { return this; }
      set { }
    }

    public IEnumerable<ITvChannel> TvChannelsSources { get; set; }
    public CancellationTokenSource ActualCancellationTokenSource { get; protected set; }


    public bool IsSelectedToPlay { get; set; }

    protected override void OnSelected(bool isSelected)
    {
      if (isSelected)
      {
        ActualCancellationTokenSource = new CancellationTokenSource();

        if (SelectedTvChannel == null)
        {
          SelectedTvChannel = SubItems.ViewModels.OfType<TvChannelItemGroupViewModel>().FirstOrDefault();
        }

        if (SelectedTvChannel != null)
        {
          var eventToPublis = eventAggregator.GetEvent<PlayChannelEvent>();

          TvChannelsSources = SubItems.ViewModels.OfType<TvChannelItemGroupViewModel>();
          SelectedTvChannel.IsSelected = true;

          eventToPublis.Publish(SelectedTvChannel);
        }
      }
    }

    public ITvItem TvItem
    {
      get
      {
        return Model?.TvItem;
      }
    }
  }

  public class TvChannelViewModel<TModel> : TreeViewItemViewModel<TModel> where TModel : class
  {
    public TvChannelViewModel(TModel model) : base(model)
    {
    }

    #region State

    private TVChannelState state = TVChannelState.Loading;

    public TVChannelState State
    {
      get { return state; }
      set
      {
        if (value != state)
        {
          state = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region URL

    private string url;

    public string Url
    {
      get { return url; }
      set
      {
        if (value != url)
        {
          url = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region BufferingValue

    private float bufferingValue;

    public float BufferingValue
    {
      get { return bufferingValue; }
      set
      {
        if (value != bufferingValue)
        {
          bufferingValue = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

   

    public virtual void RefreshSource()
    {

    }
  }
}