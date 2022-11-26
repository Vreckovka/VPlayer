using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using VCore;
using VCore.ItemsCollections;
using VCore.Standard;
using VCore.Standard.Factories.ViewModels;
using VCore.Standard.Helpers;
using VCore.WPF.Helpers;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.LRC;
using VCore.WPF.LRC.Domain;
using VCore.WPF.Misc;
using VPlayer.AudioStorage.InfoDownloader.Clients.PCloud;
using VPlayer.AudioStorage.Interfaces.Storage;

namespace VPlayer.Core.ViewModels.SoundItems.LRCCreators
{
  public class LRCCreatorViewModel : ViewModel<SongInPlayListViewModel>
  {
    private readonly SongInPlayListViewModel model;
    private readonly PCloudLyricsProvider pCloudLyricsProvider;
    private readonly IStorageManager storageManager;
    private readonly IWindowManager windowManager;
    private readonly IViewModelsFactory viewModelsFactory;

    public LRCCreatorViewModel(SongInPlayListViewModel model, PCloudLyricsProvider pCloudLyricsProvider, IStorageManager storageManager, IWindowManager windowManager, IViewModelsFactory viewModelsFactory) : base(model)
    {
      this.model = model ?? throw new ArgumentNullException(nameof(model));
      this.pCloudLyricsProvider = pCloudLyricsProvider ?? throw new ArgumentNullException(nameof(pCloudLyricsProvider));
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));

      model.ObservePropertyChange(x => x.ActualPosition)
        .ObserveOn(Application.Current.Dispatcher)
        .Subscribe(OnActualPositionChanged).DisposeWith(this);
    }

    #region Properties

    #region Lines

    private RxObservableCollection<LRCCreatorLyricsLine> lines;

    public RxObservableCollection<LRCCreatorLyricsLine> Lines
    {
      get { return lines; }
      set
      {
        if (value != lines)
        {
          lines = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

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
        Model.LRCFile = new LRCFileViewModel(lrcFile, LRCProviders.PCloud, pCloudLyricsProvider, windowManager);
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

    #region PrepareViewModel

    public void PrepareViewModel()
    {
      Lines = GetLines(Lyrics);

      if (Lines != null)
      {
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

    #region GetLines

    private RxObservableCollection<LRCCreatorLyricsLine> GetLines(string lyrics)
    {
      if (!string.IsNullOrEmpty(lyrics))
      {
        int index = 0;
        var stringLines = lyrics.Replace("\r", "").Split("\n");

        var list = new List<LRCCreatorLyricsLine>();

        if (stringLines.Length > 0)
        {
          if (stringLines[0] != "")
          {
            list.Add(new LRCCreatorLyricsLine()
            {
              Text = "",
              Index = index++
            });
          }
        }


        foreach (var stringLine in stringLines)
        {
          var newLine = new LRCCreatorLyricsLine()
          {
            Text = stringLine.Trim(),
            Index = index++
          };

          list.Add(newLine);
        }

        if (stringLines.Length > 0)
        {
          if (stringLines[stringLines.Length - 1] != "")
          {
            list.Add(new LRCCreatorLyricsLine()
            {
              Text = "",
              Index = index
            });
          }
        }

        return new RxObservableCollection<LRCCreatorLyricsLine>(list);
      }

      return null;
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

            if (!FilePlayableRegionViewModel.IsPlaying)
            {
              ActualLine.Time = Model.ActualTime;
              return;
            }

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

    public void ChangeLyrics(string newLyrics)
    {
      var oldLInes = Lines.ToList();
      var newLines = GetLines(newLyrics);

      var validOldLines = oldLInes.Where(x => x.Index != null).ToList();
   
      List<int> checkedOldIndexes = new List<int>();

      if (newLines != null)
      {
        foreach (var line in newLines.Where(x => x.Index != null))
        {
          var oldLine = validOldLines.SingleOrDefault(x => x.Index == line.Index);

          if (oldLine != null)
          {
            checkedOldIndexes.Add(oldLine.Index);

            if (string.IsNullOrEmpty(oldLine.Text) && string.IsNullOrEmpty(line.Text))
              continue;

            if (oldLine.Text != line.Text)
            {
              Lines.Insert(oldLine.Index, line);
             
              validOldLines.Where(x => x.Index >= oldLine.Index).ForEach(x => x.Index++);
            }
          }
          else
          {
            Lines.Insert(Lines.Count - 1, line);
          }
        }


        var removedLines = validOldLines.Where(x => !checkedOldIndexes.Contains(x.Index)).ToList();

        foreach (var removed in removedLines)
        {
          Lines.Remove(removed);
        }

        Lines.Sort((x, y) => x.Index.CompareTo(y.Index));
      }

    }


    public void LoadLines(LRCFileViewModel lRCFileViewModel)
    {
      var list = new List<LRCCreatorLyricsLine>();

      for (int i = 0; i < lRCFileViewModel.AllLine.Count; i++)
      {
        var x = lRCFileViewModel.AllLine[i];
        var vm = viewModelsFactory.Create<LRCCreatorLyricsLine>();

        vm.Text = x.Text;
        vm.Time = x.Model.Timestamp;
        vm.Index = i;

        list.Add(vm);
      }

      Lyrics = lRCFileViewModel.GetLyricsText();
      Lines = new RxObservableCollection<LRCCreatorLyricsLine>(list);
      ActualLine = Lines.FirstOrDefault();

      if (ActualLine != null)
      {
        ActualLine.SetIsActual(true);
      }

      Lines.ItemUpdated.ObserveOn(Application.Current.Dispatcher)
        .Where(x => x.EventArgs.PropertyName == nameof(LRCCreatorLyricsLine.IsActual))
        .Subscribe(OnActualLineChanged).DisposeWith(this);
    }

    #endregion

  }
}