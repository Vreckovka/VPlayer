using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;
using VCore;
using VCore.Standard;
using VCore.ViewModels;
using VPlayer.AudioStorage.InfoDownloader.LRC;
using VPlayer.AudioStorage.InfoDownloader.LRC.Domain;

namespace VPlayer.Core.ViewModels
{
  public class LRCFileViewModel : ViewModel<ILRCFile>
  {
    private readonly ILrcProvider lrcProvider;

    #region Fields

    private readonly ILrcProvider sourceProvider;

    #endregion

    #region Constructors

    public LRCFileViewModel(ILRCFile model, LRCProviders lRcProvider, ILrcProvider lrcProvider) : base(model)
    {
      this.lrcProvider = lrcProvider ?? throw new ArgumentNullException(nameof(lrcProvider));

      Provider = lRcProvider;

      Lines = model?.Lines.Select(x => new LRCLyricLineViewModel(x)).ToList();

      if (Lines != null)
      {
        var last = Lines.LastOrDefault();
        var first = Lines.FirstOrDefault();

        if (last != null && string.IsNullOrEmpty(last.Text))
        {
          Lines.Add(new LRCLyricLineViewModel(new LRCLyricLine()
          {
            Text = "(End)",
            Timestamp = last.Model.Timestamp + TimeSpan.FromSeconds(1)
          }));
        }

        if (first != null && first.Model.Timestamp > TimeSpan.FromSeconds(0))
        {

          Lines.Insert(0, new LRCLyricLineViewModel(new LRCLyricLine()
          {
            Text = null,
            Timestamp = TimeSpan.FromSeconds(0)
          }));
        }
      }
    }

    #endregion

    #region ActualSongChanged

    private ReplaySubject<int> actualLineSubject = new ReplaySubject<int>(1);

    public IObservable<int> ActualLineChanged
    {
      get { return actualLineSubject.AsObservable(); }
    }

    #endregion

    public LRCProviders Provider { get; private set; }
    public List<LRCLyricLineViewModel> Lines { get; }
    public LRCLyricLineViewModel ActualLine { get; private set; }

    #region TimeAdjustment

    private double timeAdjustment;
    public double TimeAdjustment
    {
      get { return timeAdjustment; }
      set
      {
        if (value != timeAdjustment)
        {
          timeAdjustment = value;
          RaisePropertyChanged();
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

          var newLine = Lines.Where(x => x.Model.Timestamp != null &&
                                         x.Model.Timestamp.Value.TotalMilliseconds + TimeAdjustment <= timeSpan.TotalMilliseconds).OrderByDescending(x => x.Model.Timestamp).FirstOrDefault();

          if (newLine != null && ActualLine != newLine)
          {
            newLine.IsActual = true;
            var oldIndex = Lines.IndexOf(newLine);

            if (oldIndex + 1 < Lines.Count)
            {
              var oldTimestamp = Lines[oldIndex].Model.Timestamp;

              var nextTimestampIndex = oldIndex;
              
              do
              {
                nextTimestampIndex++;

                var nextLineTimestamp = Lines[nextTimestampIndex].Model.Timestamp;

                if (nextTimestampIndex < Lines.Count && nextLineTimestamp.HasValue)
                {
                  nextTimestamp = TimeSpan.FromMilliseconds(nextLineTimestamp.Value.TotalMilliseconds + TimeAdjustment);
                }
                else
                {
                  nextTimestamp = null;
                  break;
                }


              } while (nextTimestamp == oldTimestamp && nextTimestampIndex + 1 < Lines.Count);


            }
            else
            {
              nextTimestamp = null;
            }

            lastTimestamp = timeSpan;

          }

          ActualLine = newLine;

          if (newLine != null)
            actualLineSubject.OnNext(Lines.IndexOf(newLine));
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
      Model.Lines.ForEach(x => x.Timestamp += TimeSpan.FromMilliseconds(TimeAdjustment));

      if (Model is GoogleLRCFile google && Provider == LRCProviders.Google)
      {
        lrcProvider.Update(Model);
      }
      else
      {
       var lrcFile = await lrcProvider.TryGetLrcAsync(Model.Title, Model.Artist, Model.Album);
      
       Model = lrcFile;

       lrcProvider.Update(lrcFile);
      }
    
    }

    #endregion
  }
}