using System;
using System.Threading;
using System.Threading.Tasks;
using VCore.Standard;
using VCore.Standard.ViewModels.TreeView;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPLayer.Domain.Contracts.IPTV;

namespace VPlayer.IPTV.ViewModels
{
  public class TvChannelViewModel : TvChannelViewModel<TvChannel>
  {
    public TvChannelViewModel(TvChannel model) : base(model)
    {
      Url = model.TvItem.Source;
      Name = model.Name;
    }

    public virtual Task<string> InitilizeUrl(CancellationToken cancellationToken)
    {
      return Task.Run(() => { return Model.TvItem.Source; });
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