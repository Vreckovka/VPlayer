using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using Prism.Events;
using VCore.Standard;
using VCore.WPF.Misc;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.AudioStorage.Scrappers.CSFD.Domain;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels.SoundItems;

namespace VPlayer.Core.ViewModels.TvShows
{
  public class CSFDItemViewModel : ViewModel<CSFDItem>
  {
    public CSFDItemViewModel(CSFDItem model) : base(model)
    {
    }

    #region OpenCsfd

    private ActionCommand openCsfd;
    public ICommand OpenCsfd
    {
      get
      {
        return openCsfd ??= new ActionCommand(OnOpenCsfd);
      }
    }

    private void OnOpenCsfd()
    {
      if (!string.IsNullOrEmpty(Model.Url))
      {
        Process.Start(new ProcessStartInfo()
        {
          FileName = Model.Url,
          UseShellExecute = true,
          Verb = "open"
        });
      }
    }

    #endregion

    #region Name

    public string Name
    {
      get { return Model.Name; }
      set
      {
        if (value != Model.Name)
        {
          Model.Name = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region OriginalName

    public string OriginalName
    {
      get { return Model.OriginalName; }
      set
      {
        if (value != Model.OriginalName)
        {
          Model.OriginalName = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Year

    public int? Year
    {
      get { return Model.Year; }
      set
      {
        if (value != Model.Year)
        {
          Model.Year = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ImagePath

    public string ImagePath
    {
      get { return Model.ImagePath; }
      set
      {
        if (value != Model.ImagePath)
        {
          Model.ImagePath = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Rating

    public int? Rating
    {
      get { return Model.Rating; }
      set
      {
        if (value != Model.Rating)
        {
          Model.Rating = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region RatingColor

    public RatingColor? RatingColor
    {
      get { return Model.RatingColor; }
      set
      {
        if (value != Model.RatingColor)
        {
          Model.RatingColor = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

  }

  public class VideoItemInPlaylistViewModel : FileItemInPlayList<VideoItem>
  {
    public VideoItemInPlaylistViewModel(VideoItem model, IEventAggregator eventAggregator, IStorageManager storageManager) : base(model, eventAggregator, storageManager)
    {
      Description = Path.GetDirectoryName(model.Source);
    }

    protected override void PublishPlayEvent()
    {
      eventAggregator.GetEvent<PlaySongsFromPlayListEvent<VideoItemInPlaylistViewModel>>().Publish(this);
    }

    #region Description

    private string description;

    public string Description
    {
      get { return description; }
      set
      {
        if (value != description)
        {
          description = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region CSFDItem

    private CSFDItemViewModel cSFDItem;

    public CSFDItemViewModel CSFDItem
    {
      get { return cSFDItem; }
      set
      {
        if (value != cSFDItem)
        {
          cSFDItem = value;
          ExtraData = value;

          RaisePropertyChanged();
          RaiseNotifyPropertyChanged(nameof(ImagePath));
        }
      }
    }

    #endregion

    public bool IsStream { get; set; }

    public string ImagePath => CSFDItem?.Model?.ImagePath;

    protected override void PublishRemoveFromPlaylist()
    {
      var songs = new List<VideoItemInPlaylistViewModel>() { this };

      var args = new RemoveFromPlaylistEventArgs<VideoItemInPlaylistViewModel>()
      {
        DeleteType = DeleteType.SingleFromPlaylist,
        ItemsToRemove = songs
      };

      eventAggregator.GetEvent<RemoveFromPlaylistEvent<VideoItemInPlaylistViewModel>>().Publish(args);
    }

    protected override void PublishDeleteFile()
    {
      var songs = new List<VideoItemInPlaylistViewModel>() { this };

      var args = new RemoveFromPlaylistEventArgs<VideoItemInPlaylistViewModel>()
      {
        DeleteType = DeleteType.File,
        ItemsToRemove = songs
      };

      eventAggregator.GetEvent<RemoveFromPlaylistEvent<VideoItemInPlaylistViewModel>>().Publish(args);
    }

    protected override void OnDownloadInfo()
    {
      eventAggregator.GetEvent<DownloadInfoEvent<VideoItemInPlaylistViewModel>>().Publish(this);
    }
  }
}