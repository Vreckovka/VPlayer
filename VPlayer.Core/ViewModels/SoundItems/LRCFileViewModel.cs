using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Input;
using VCore;
using VCore.ItemsCollections.VirtualList.VirtualLists;
using VCore.Standard;
using VPlayer.AudioStorage.InfoDownloader.Clients.PCloud;
using VPlayer.AudioStorage.InfoDownloader.LRC;
using VPlayer.AudioStorage.InfoDownloader.LRC.Clients.Google;
using VPlayer.AudioStorage.InfoDownloader.LRC.Domain;

namespace VPlayer.Core.ViewModels.SoundItems
{
  public class LRCFileViewModel : ViewModel<ILRCFile>
  {
    #region Fields

    private readonly PCloudLyricsProvider pCloudLyricsProvider;
    private readonly ILrcProvider sourceProvider;

    #endregion

    #region Constructors

    public LRCFileViewModel(
      ILRCFile model, 
      LRCProviders lRcProvider,
      PCloudLyricsProvider pCloudLyricsProvider, 
      ILrcProvider lrcProvider = null) : base(model)
    {
      this.pCloudLyricsProvider = pCloudLyricsProvider ?? throw new ArgumentNullException(nameof(pCloudLyricsProvider));

      sourceProvider = lrcProvider;
      Provider = lRcProvider;

      AllLine = model?.Lines.Select(x => new LRCLyricLineViewModel(x)).ToList();

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

    #region SetActualLine

    private TimeSpan? lastTimestamp;
    private TimeSpan? nextTimestamp;
    private object batton = new object();

    public void SetActualLine(TimeSpan timeSpan)
    {
      lock (batton)
      {
        if (lastTimestamp == null ||
            (lastTimestamp <= timeSpan && nextTimestamp <= timeSpan) ||
            (lastTimestamp > timeSpan))
        {
          if (ActualLine != null)
          {
            ActualLine.IsActual = false;
          }

          var newLine = AllLine.Where(x => x.Model.Timestamp != null &&
                                           x.Model.Timestamp.Value.TotalMilliseconds + TimeAdjustment <= timeSpan.TotalMilliseconds).OrderByDescending(x => x.Model.Timestamp).FirstOrDefault();

          if (newLine != null && ActualLine != newLine)
          {
            newLine.IsActual = true;
            var oldIndex = AllLine.IndexOf(newLine);

            if (oldIndex + 1 < AllLine.Count)
            {
              var oldTimestamp = AllLine[oldIndex].Model.Timestamp;

              var nextTimestampIndex = oldIndex;

              do
              {
                nextTimestampIndex++;

                var nextLineTimestamp = AllLine[nextTimestampIndex].Model.Timestamp;

                if (nextTimestampIndex < AllLine.Count && nextLineTimestamp.HasValue)
                {
                  nextTimestamp = TimeSpan.FromMilliseconds(nextLineTimestamp.Value.TotalMilliseconds + TimeAdjustment);
                }
                else
                {
                  nextTimestamp = null;
                  break;
                }


              } while (nextTimestamp == oldTimestamp && nextTimestampIndex + 1 < AllLine.Count);


            }
            else
            {
              nextTimestamp = null;
            }

            lastTimestamp = timeSpan;

          }

          ActualLine = newLine;

          if (ActualLine != null)
          {
            actualLineSubject.OnNext(AllLine.IndexOf(ActualLine));
          }
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

        Application.Current.Dispatcher.Invoke(() => { IsLoading = true; UpdateStatus = null; });

        var result = await pCloudLyricsProvider.Update(Model);

        Application.Current.Dispatcher.Invoke(() =>
        {
          UpdateStatus = result;

          if (UpdateStatus == true)
          {
            Provider = LRCProviders.PCloud;
            TimeAdjustment = 0;
          }
        });
      }
      finally
      {
        Application.Current.Dispatcher.Invoke(() => { IsLoading = false; });
      }

    }

    #endregion
  }
}