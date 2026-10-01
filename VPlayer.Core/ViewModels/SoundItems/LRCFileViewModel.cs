using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Input;
using TagLib.Tiff.Pef;
using VCore;
using VCore.Standard;
using VCore.Standard.Helpers;
using VCore.WPF;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.ItemsCollections.VirtualList.VirtualLists;
using VCore.WPF.LRC;
using VCore.WPF.LRC.Domain;
using VCore.WPF.Misc;
using VPlayer.AudioStorage.InfoDownloader.Clients.PCloud;
using VPlayer.AudioStorage.InfoDownloader.LRC;
using VPlayer.AudioStorage.InfoDownloader.LRC.Clients;
using VPlayer.AudioStorage.InfoDownloader.LRC.Clients.Google;
using VPlayer.Core.ViewModels.SoundItems.LRCCreators;

namespace VPlayer.Core.ViewModels.SoundItems
{
  public class LRCFileViewModel : ViewModel<ILRCFile>
  {
    #region Fields

    private readonly PCloudLyricsProvider pCloudLyricsProvider;
    private readonly ILrcProvider sourceProvider;
    private readonly IWindowManager windowManager;

    #endregion

    #region Constructors

    public LRCFileViewModel(
      ILRCFile model,
      LRCProviders lRcProvider,
      PCloudLyricsProvider pCloudLyricsProvider,
      IWindowManager windowManager,
      ILrcProvider lrcProvider = null) : base(model)
    {
      this.pCloudLyricsProvider = pCloudLyricsProvider ?? throw new ArgumentNullException(nameof(pCloudLyricsProvider));

      sourceProvider = lrcProvider;
      this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
      Provider = lRcProvider;

      AllLine = model?.Lines?.Select(x => new LRCLyricLineViewModel(x)).ToList() ?? new List<LRCLyricLineViewModel>();

      if (AllLine != null)
      {
        var last = AllLine.LastOrDefault();
        var first = AllLine.FirstOrDefault();

        if (last != null && string.IsNullOrEmpty(last.Text))
        {
          AllLine.Add(new LRCLyricLineViewModel(new LRCLyricLine()
          {
            Text = "(End)",
            Timestamp = last.Model.Timestamp + TimeSpan.FromSeconds(1)
          }));
        }

        if (first != null && first.Model.Timestamp > TimeSpan.FromSeconds(0))
        {

          AllLine.Insert(0, new LRCLyricLineViewModel(new LRCLyricLine()
          {
            Text = null,
            Timestamp = TimeSpan.FromSeconds(0)
          }));
        }
      }

      LinesView = new VirtualList<LRCLyricLineViewModel>(AllLine, 5);


    }

    #endregion

    #region ActualSongChanged

    private ReplaySubject<int> actualLineSubject = new ReplaySubject<int>(1);

    public IObservable<int> ActualLineChanged
    {
      get { return actualLineSubject.AsObservable(); }
    }

    #endregion

    #region Provider

    private LRCProviders provider;

    public LRCProviders Provider
    {
      get { return provider; }
      set
      {
        if (value != provider)
        {
          provider = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public List<LRCLyricLineViewModel> AllLine { get; set; }
    public VirtualList<LRCLyricLineViewModel> LinesView { get; }

    #region LyricsColor

    private string lyricsColor = "#fec827";

    public string LyricsColor
    {
      get { return lyricsColor; }
      set
      {
        if (value != lyricsColor)
        {
          lyricsColor = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsMenuOpened

    private bool isMenuOpened;

    public bool IsMenuOpened
    {
      get { return isMenuOpened; }
      set
      {
        if (value != isMenuOpened)
        {
          isMenuOpened = value;
          UpdateStatus = null;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region PinMenu

    private bool pinMenu;

    public bool PinMenu
    {
      get { return pinMenu; }
      set
      {
        if (value != pinMenu)
        {
          pinMenu = value;

          if (!pinMenu)
          {
            IsMenuOpened = false;
          }

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ActualLine

    private LRCLyricLineViewModel actualLine;

    public LRCLyricLineViewModel ActualLine
    {
      get { return actualLine; }
      private set
      {
        if (value != actualLine)
        {
          actualLine = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TimeAdjustmentSeconds

    public double TimeAdjustmentSeconds
    {
      get { return timeAdjustment / 1000.0; }

    }

    #endregion

    #region TimeAdjustment

    private double timeAdjustment = 0;
    public double TimeAdjustment
    {
      get { return timeAdjustment; }
      set
      {
        if (value != timeAdjustment)
        {
          timeAdjustment = value;
          RaisePropertyChanged();
          RaisePropertyChanged(nameof(TimeAdjustmentSeconds));
        }
      }
    }

    #endregion

    #region IsLoading

    private bool isLoading;

    public bool IsLoading
    {
      get { return isLoading; }
      set
      {
        if (value != isLoading)
        {
          isLoading = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region UpdateStatus

    private bool? updateStatus = null;

    public bool? UpdateStatus
    {
      get { return updateStatus; }
      set
      {
        if (value != updateStatus)
        {
          updateStatus = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Commands

    #region ApplyPernamently

    private ActionCommand applyPernamently;

    public ICommand ApplyPernamently
    {
      get
      {
        if (applyPernamently == null)
        {
          applyPernamently = new ActionCommand(OnApplyPernamently);
        }

        return applyPernamently;
      }
    }

    public async void OnApplyPernamently()
    {
      try
      {

        Model.Lines.ForEach(x => x.Timestamp += TimeSpan.FromMilliseconds(TimeAdjustment));

        VSynchronizationContext.PostOnUIThread(() => { IsLoading = true; UpdateStatus = null; });

        var result = await pCloudLyricsProvider.Update(Model);
        if (!result)
        {
          //SaveLocally(Model);

          var provider = new LocalLrcProvider("C:\\Lyrics");
          await provider.Update(Model);
        }

        VSynchronizationContext.PostOnUIThread(() =>
        {
          UpdateStatus = result;

          if (UpdateStatus == true)
          {
            LinesView.Where(x => x != null).ForEach(x => x.RaiseNotification());
            Provider = LRCProviders.PCloud;
            TimeAdjustment = 0;
          }
        });
      }
      finally
      {
        VSynchronizationContext.PostOnUIThread(() => { IsLoading = false; });
      }

    }

    #endregion


    public string GetLyricsText()
    {
      if (AllLine.Any())
      {
        return string.Join("\n", AllLine.Select(x => x.Text));
      }

      return null;
    }

    #endregion

    #region SetActualLine


    public void SetActualLine(TimeSpan timeSpan)
    {
      var newLine = AllLine
        .Where(x => x.Model.Timestamp.HasValue &&
          x.Model.Timestamp.Value.TotalMilliseconds + TimeAdjustment <= timeSpan.TotalMilliseconds)
        .OrderByDescending(x => x.Model.Timestamp).FirstOrDefault();

      if (ActualLine == newLine)
        return;

      if (ActualLine != null)
        ActualLine.IsActual = false;
      ActualLine = newLine;
      if (ActualLine != null)
        ActualLine.IsActual = true;

      actualLineSubject.OnNext(ActualLine == null ? -1 : AllLine.IndexOf(ActualLine));
    }

    #endregion
  }
}
