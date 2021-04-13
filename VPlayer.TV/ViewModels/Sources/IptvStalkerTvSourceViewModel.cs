using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using IPTVStalker;
using Microsoft.EntityFrameworkCore;
using VCore.Standard.Factories.ViewModels;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.Managers.Status;

namespace VPlayer.IPTV.ViewModels
{
  public class IptvStalkerTvSourceViewModel : TVSourceViewModel
  {
    private readonly IStatusManager statusManager;
    private IPTVStalkerService serviceStalker;
    private ConnectionProperties connectionProperties;
    private string cacheFolder;

    public IptvStalkerTvSourceViewModel(
      TvSource tVSource,
      TVPlayerViewModel player,
      IStorageManager storageManager,
      IViewModelsFactory viewModelsFactory,
      IStatusManager statusManager) : base(tVSource, player, storageManager, viewModelsFactory)
    {
      if (tVSource.TvSourceType != TVSourceType.IPTVStalker)
      {
        throw new ArgumentException("Wrong source type");
      }

      this.statusManager = statusManager ?? throw new ArgumentNullException(nameof(statusManager));

      cacheFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $"VPlayer\\IPTVStalkers\\{Name}");

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
        connectionProperties = new ConnectionProperties()
        {
          TimeZone = "GMT",
          MAC = MacAddress,
          Server = Url,
        };
      }

      if (connectionProperties != null)
        serviceStalker = new IPTVStalkerService(connectionProperties);
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
        connectionProperties = new ConnectionProperties()
        {
          TimeZone = "GMT",
          MAC = MacAddress,
          Server = Url,
        };
      }

      if (serviceStalker == null)
      {
        if (connectionProperties != null)
        {
          serviceStalker = new IPTVStalkerService(connectionProperties);

          var statusMessage = new StatusMessage(1)
          {
            ActualMessageStatusState = MessageStatusState.Processing,
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
              statusMessage.ActualMessageStatusState = MessageStatusState.Failed;
              statusManager.UpdateMessageAndIncreaseProcessCount(statusMessage);
            }
          });
        }


        var statusMessage1 = new StatusMessage(1)
        {
          ActualMessageStatusState = MessageStatusState.Processing,
          Message = $"Storing tv channels",
          NumberOfProcesses = serviceStalker.Channels.data.Count
        };

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
          statusManager.UpdateMessage(statusMessage1);
        });

        int count = 0;
        int range = 100;

        if (serviceStalker != null)
        {
          Model.TvChannels = new System.Collections.Generic.List<TvChannel>();

          foreach (var channel in serviceStalker.Channels.data)
          {
            var tvChannel = new TvChannel()
            {
              Name = channel.name,
              Url = channel.cmd,
              IdTvSource = Model.Id
            };

            Model.TvChannels.Add(tvChannel);
          }

          for (int i = 0; i < Model.TvChannels.Count; i += range)
          {
            storageManager.StoreRangeEntity<TvChannel>(Model.TvChannels.Skip(i).Take(range).ToList());


            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
              statusMessage1.ProcessedCount = i;
              statusManager.UpdateMessage(statusMessage1);
            });

          }
        }

        await Application.Current.Dispatcher.InvokeAsync(() =>
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
    }

    #endregion

    #region LoadChannels

    public override void LoadChannels()
    {
      if (serviceStalker != null)
      {
        var dbEntity = storageManager.GetRepository<TvSource>().Include(x => x.TvChannels).SingleOrDefault(x => x.Id == Model.Id);

        if (dbEntity != null)
        {
          foreach (var channel in dbEntity.TvChannels)
          {
            TvChannels.Add(viewModelsFactory.Create<TvStalkerChannelViewModel>(channel, serviceStalker));
          }
        }

        serviceStalker.Prepare();
      }

     
    }

    #endregion
  }
}
