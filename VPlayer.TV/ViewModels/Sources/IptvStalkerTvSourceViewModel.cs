using System;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using IPTVStalker;
using Microsoft.EntityFrameworkCore;
using Prism.Events;
using VCore;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF;
using VCore.WPF.Controls.StatusMessage;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Managers;
using VCore.WPF.Misc;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Managers.Status;
using VPLayer.Domain.Contracts.IPTV;

namespace VPlayer.IPTV.ViewModels
{
  public class IptvStalkerTvSourceViewModel : TVSourceViewModel
  {
    private readonly IStatusManager statusManager;
    private readonly IIptvStalkerServiceProvider iptvStalkerServiceProvider;
    private IPTVStalkerService serviceStalker;

    public IptvStalkerTvSourceViewModel(
      TvSource tVSource,
      IStorageManager storageManager,
      IViewModelsFactory viewModelsFactory,
      IStatusManager statusManager,
      IIptvStalkerServiceProvider iptvStalkerServiceProvider,
      IEventAggregator eventAggregator,
      IWindowManager windowManager) : base(tVSource, storageManager, viewModelsFactory, eventAggregator, windowManager)
    {
      if (tVSource.TvSourceType != TVSourceType.IPTVStalker)
      {
        throw new ArgumentException("Wrong source type");
      }

      this.statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));
      this.iptvStalkerServiceProvider = iptvStalkerServiceProvider ?? throw new ArgumentNullException(nameof(iptvStalkerServiceProvider));

      if ((string.IsNullOrEmpty(Url) || string.IsNullOrEmpty(MacAddress)) && Model.SourceConnection != null)
      {
        var split = Model.SourceConnection.Split("|");

        if (split.Length == 2)
        {
          Url = split[0];
          MacAddress = split[1];
        }
      }

      if (!string.IsNullOrEmpty(Url) && !string.IsNullOrEmpty(MacAddress))
      {
        serviceStalker = iptvStalkerServiceProvider.GetStalkerService(Url, MacAddress);
      }
    }

    #region ReloadTvChannels

    private ActionCommand reloadTvChannels;

    public ICommand ReloadTvChannels
    {
      get
      {
        if (reloadTvChannels == null)
        {
          reloadTvChannels = new ActionCommand(OnReloadTvChannels);
        }

        return reloadTvChannels;
      }
    }

    public void OnReloadTvChannels()
    {
      LoadService();
    }

    #endregion

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

    #region MacAddress

    private string macAddress;

    public string MacAddress
    {
      get { return macAddress; }
      set
      {
        if (value != macAddress)
        {
          macAddress = value;

          ValidateIsValid();

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region PrepareEntityForDb

    public override Task PrepareEntityForDb()
    {
      return Task.Run(async () =>
      {
        Model.SourceConnection = Url + "|" + MacAddress;

        LoadService();
      });
    }

    #endregion

    #region ValidateIsValid

    public override void ValidateIsValid()
    {
      if (!string.IsNullOrEmpty(Url) && !string.IsNullOrEmpty(MacAddress) && !string.IsNullOrEmpty(Name))
      {
        IsValid = true;
      }
      else
      {
        IsValid = false;
      }
    }

    #endregion

    #region LoadService

    private async void LoadService()
    {
      TvChannels.Clear();

      if (string.IsNullOrEmpty(Url) || string.IsNullOrEmpty(MacAddress))
      {
        var split = Model.SourceConnection.Split("|");

        if (split.Length == 2)
        {
          Url = split[0];
          MacAddress = split[1];
        }
      }

      if (!string.IsNullOrEmpty(Url) && !string.IsNullOrEmpty(MacAddress))
      {
        serviceStalker = iptvStalkerServiceProvider.GetStalkerService(Url, MacAddress);
      }


      var statusMessage = new StatusMessageViewModel(1)
      {
        Status = StatusType.Processing,
        Message = $"Fetching tv channels for stalker service {Name}"
      };

      statusManager.UpdateMessage(statusMessage);

      await Task.Run(() =>
      {
        try
        {
          if (TvChannels.ViewModels.Count == 0)
            serviceStalker.FetchData();
        }
        catch (Exception ex)
        {
          statusMessage.Status = StatusType.Error;
          statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);
        }
      });


      var statusMessage1 = new StatusMessageViewModel(1)
      {
        Status = StatusType.Processing,
        Message = $"Storing tv channels",
        NumberOfProcesses = serviceStalker.Channels.data.Count
      };

      VSynchronizationContext.PostOnUIThread(() =>
      {
        statusManager.UpdateMessage(statusMessage1);
      });

      int count = 0;
      int range = 250;

      if (serviceStalker != null)
      {
        Model.TvChannels = new System.Collections.Generic.List<TvChannel>();

        foreach (var channel in serviceStalker.Channels.data)
        {
          var tvItem = new TvItem()
          {
            Source = channel.cmd,
            Name = channel.name
          };

          var tvChannel = new TvChannel()
          {
            TvItem = tvItem,
            IdTvSource = Model.Id
          };

          Model.TvChannels.Add(tvChannel);
        }

        for (int i = 0; i < Model.TvChannels.Count; i += range)
        {
          storageManager.StoreRangeEntity<TvChannel>(Model.TvChannels.Skip(i).Take(range).ToList());


          VSynchronizationContext.PostOnUIThread(() =>
          {
            statusMessage1.ProcessedCount = i;
            statusManager.UpdateMessage(statusMessage1);
          });

        }
      }

      VSynchronizationContext.PostOnUIThread(() =>
      {
        statusMessage1.ProcessedCount = statusMessage1.NumberOfProcesses;

        statusManager.UpdateMessage(statusMessage1);

        foreach (var channel in Model.TvChannels)
        {
          channel.TvSource = Model;

          TvChannels.Add(viewModelsFactory.Create<TvStalkerChannelViewModel>(channel, serviceStalker));
        }
      });
    }

    #endregion

    #region LoadChannels

    public override void LoadChannels()
    {
      if (serviceStalker != null)
      {
        var dbEntity = storageManager.GetTempRepository<TvSource>().Include(x => x.TvChannels).ThenInclude(x => x.TvItem).SingleOrDefault(x => x.Id == Model.Id);

        if (dbEntity != null)
        {
          foreach (var channel in dbEntity.TvChannels)
          {
            TvChannels.Add(viewModelsFactory.Create<TvStalkerChannelViewModel>(channel, serviceStalker));
          }
        }
      }
    }

    #endregion
  }
}
