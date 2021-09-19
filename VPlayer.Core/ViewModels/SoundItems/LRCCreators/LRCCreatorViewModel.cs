using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using VCore;
using VCore.Helpers;
using VCore.ItemsCollections;
using VCore.Standard;
using VCore.Standard.Helpers;
using VPlayer.AudioStorage.InfoDownloader.LRC;
using VPlayer.AudioStorage.InfoDownloader.LRC.Clients.Google;
using VPlayer.AudioStorage.InfoDownloader.LRC.Domain;
using VPlayer.AudioStorage.Interfaces.Storage;

namespace VPlayer.Core.ViewModels.SoundItems.LRCCreators
{
  public class LRCCreatorViewModel : ViewModel<SongInPlayListViewModel>
  {
    private readonly SongInPlayListViewModel model;
    private readonly PCloudLyricsProvider pCloudLyricsProvider;
    private readonly IStorageManager storageManager;

    public LRCCreatorViewModel(SongInPlayListViewModel model, PCloudLyricsProvider pCloudLyricsProvider, IStorageManager storageManager) : base(model)
    {
      this.model = model ?? throw new ArgumentNullException(nameof(model));
      this.pCloudLyricsProvider = pCloudLyricsProvider ?? throw new ArgumentNullException(nameof(pCloudLyricsProvider));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));

      model.ObservePropertyChange(x => x.ActualPosition)
        .ObserveOn(Application.Current.Dispatcher)
        .Subscribe(OnActualPositionChanged).DisposeWith(this);
    }

    #region Properties

    public RxObservableCollection<LRCCreatorLyricsLine> Lines { get; set; }

    public IFilePlayableRegionViewModel FilePlayableRegionViewModel { get; set; }

    #region Lyrics

    private string lyrics;

    public string Lyrics
    {
      get { return lyrics; }
      set
      {
        if (value != lyrics)
        {
          lyrics = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ActualLine

    private LRCCreatorLyricsLine actualLine;

    public LRCCreatorLyricsLine ActualLine
    {
      get { return actualLine; }
      set
      {
        if (value != actualLine)
        {
          actualLine = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion

    #region Commands

    #region SetActualLine

    private ActionCommand<LRCCreatorLyricsLine> refresh;

    public ICommand SetActualLine
    {
      get
      {
        if (refresh == null)
        {
          refresh = new ActionCommand<LRCCreatorLyricsLine>(OnSetActualLine);
        }

        return refresh;
      }
    }


    private void OnSetActualLine(LRCCreatorLyricsLine lRCCreatorLyricsLine)
    {
      lRCCreatorLyricsLine.IsActual = true;

      if (lRCCreatorLyricsLine.Time != null)
      {
        float value = (float)(lRCCreatorLyricsLine.Time.Value.TotalSeconds * 100.0f / Model.Duration) / 100.0f;

        if (FilePlayableRegionViewModel != null)
        {
          FilePlayableRegionViewModel.SetMediaPosition(value);
        }
      }
    }

    #endregion

    #region OnSaveLRC

    private ActionCommand saveLRC;

    public ICommand SaveLRC
    {
      get
      {
        if (saveLRC == null)
        {
          saveLRC = new ActionCommand(OnSaveLRC);
        }

        return saveLRC;
      }
    }


    private void OnSaveLRC()
    {
      var lrcLines = new List<LRCLyricLine>();

      foreach (var line in Lines)
      {
        if (line.Time != null)
          lrcLines.Add(new LRCLyricLine()
          {
            Text = line.Text.Trim(),
            Timestamp = line.Time
          });
      }

      var lrcFile = new LRCFile(lrcLines)
      {
        Album = Model?.AlbumViewModel?.Name,
        Artist = Model?.ArtistViewModel?.Name,
        By = "Vreckovka",
        Title = Model?.Name
      };

      if (Model != null)
      {
        Model.LRCFile = new LRCFileViewModel(lrcFile, LRCProviders.PCloud, pCloudLyricsProvider);
        Model.LRCFile.OnApplyPernamently();
        Model.LRCCreatorViewModel = null;

        if (Model.SongModel.Id > 0)
        {
          Model.SongModel.LRCLyrics = (int)Model.LRCFile.Provider + ";" + lrcFile.GetString();
          storageManager.UpdateEntityAsync(Model.SongModel);
        }

        Model.RaiseLyricsChange();

        if (FilePlayableRegionViewModel != null)
        {
          FilePlayableRegionViewModel.PlayNextItemOnEndReached = true;
        }
      }
    }

    #endregion

    #endregion

    #region Methods

    #region PrepareLyrics

    public void PrepareLyrics()
    {
      if (!string.IsNullOrEmpty(Lyrics))
      {
        var stringLines = Lyrics.Replace("\r", "").Split("\n");

        var list = new List<LRCCreatorLyricsLine>();

        if (stringLines.Length > 0)
        {
          if (stringLines[0] != "")
          {
            list.Add(new LRCCreatorLyricsLine()
            {
              Text = ""
            });
          }
        }
        

        foreach (var stringLine in stringLines)
        {
          var newLine = new LRCCreatorLyricsLine()
          {
            Text = stringLine.Trim()
          };

          list.Add(newLine);
        }

        if (stringLines.Length > 0)
        {
          if (stringLines[stringLines.Length - 1] != "")
          {
            list.Add(new LRCCreatorLyricsLine()
            {
              Text = ""
            });
          }
        }


        Lines = new RxObservableCollection<LRCCreatorLyricsLine>(list);

        ActualLine = Lines.FirstOrDefault();

        if (ActualLine != null)
        {
          ActualLine.SetIsActual(true);
          ActualLine.Time = new TimeSpan(0);
        }
         

        Lines.ItemUpdated.ObserveOn(Application.Current.Dispatcher)
          .Where(x => x.EventArgs.PropertyName == nameof(LRCCreatorLyricsLine.IsActual))
          .Subscribe(OnActualLineChanged).DisposeWith(this);
      }
    }

    #endregion

    #region OnActualPositionChanged

    private void OnActualPositionChanged(float actualItem)
    {
      //if (ActualLine != null)
      //{
      //  var seconds = actualItem * Model.Duration;

      //  if (TimeSpan.MaxValue.TotalSeconds > seconds)
      //  {
      //    ActualLine.Time = TimeSpan.FromSeconds(seconds);
      //  }
      //}
    }

    #endregion

    #region OnActualLineChanged

    private void OnActualLineChanged(EventPattern<PropertyChangedEventArgs> x)
    {
      if (x.Sender is LRCCreatorLyricsLine line)
      {
        if (line.IsActual)
        {
          if (ActualLine != line && ActualLine != null)
          {
            ActualLine.IsActual = false;
          }

          ActualLine = line;

          if (ActualLine != null && ActualLine.Time == null)
            ActualLine.Time = Model.ActualTime;
        }
        else
        {
          if (ActualLine == line)
          {
            var index = Lines.IndexOf(ActualLine);

            if (index + 1 < Lines.Count)
            {
              ActualLine = Lines[index + 1];
              ActualLine.IsActual = true;
              ActualLine.Time = Model.ActualTime;
            }
            else
            {
              if (ActualLine == line)
                ActualLine = null;
            }
          }
        }
      }
    }

    #endregion 

    #endregion
  }
}