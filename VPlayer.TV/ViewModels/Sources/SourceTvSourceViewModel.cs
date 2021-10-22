using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Prism.Events;

using VCore.Standard.Factories.ViewModels;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Managers;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Events;
using VPLayer.Domain.Contracts.IPTV;

namespace VPlayer.IPTV.ViewModels
{
  public class SourceTvSourceViewModel : TVSourceViewModel
  {

    public SourceTvSourceViewModel(
      TvSource tVSource,
      IStorageManager storageManager,
      IViewModelsFactory viewModelsFactory,
      IEventAggregator eventAggregator,
      IWindowManager windowManager) : base(tVSource, storageManager, viewModelsFactory, eventAggregator, windowManager)
    {
      if (tVSource.TvSourceType != TVSourceType.Source)
      {
        throw new ArgumentException("Wrong source type");
      }

      Url = tVSource.SourceConnection;

     
    }

    #region Url

    private string url;

    public string Url
    {
      get { return url; }
      set
      {
        if (value != url)
        {
          url = value;

          ValidateIsValid();

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region PrepareEntityForDb

    public override Task PrepareEntityForDb()
    {
      return Task.Run(() =>
      {
        Model.SourceConnection = Url;

        var tvItem = new TvItem()
        {
          Source = Url,
          Name = Name
        };

        Model.TvChannels = new List<TvChannel>()
        {
          new TvChannel()
          {
            TvSource = Model,
            TvItem = tvItem
          }
        };
      });
    }

    #endregion

    #region ValidateIsValid

    public override void ValidateIsValid()
    {
      if (!string.IsNullOrEmpty(Url) && !string.IsNullOrEmpty(Name))
      {
        IsValid = true;
      }
      else
      {
        IsValid = false;
      }
    }

    #endregion

    //#region OnSelected

    //protected override void OnSelected()
    //{
    //  if (IsSelected)
    //  {
    //    var channel = TvChannels.View.SingleOrDefault();

    //    if (channel != null)
    //    {
    //      eventAggregator.GetEvent<PlayItemsEvent<TvPlaylistItem, TvItemInPlaylistItemViewModel>>().Publish(new PlayItemsEventData<TvItemInPlaylistItemViewModel>(new List<TvItemInPlaylistItemViewModel>()
    //      {
    //        viewModelsFactory.Create<TvItemInPlaylistItemViewModel>(new TvPlaylistItem()
    //        {
    //          TvChannel = channel.Model,
    //          Name = channel.Name
    //        })
    //      }, EventAction.Play, this));
    //    }
    //  }
    //}

    //#endregion
  }
}