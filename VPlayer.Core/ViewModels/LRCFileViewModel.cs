using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using VCore.ViewModels;
using VPlayer.AudioStorage.InfoDownloader.LRC;
using VPlayer.AudioStorage.InfoDownloader.LRC.Domain;

namespace VPlayer.Core.ViewModels
{
  public class LRCFileViewModel : ViewModel<LRCFile>
  {
    #region Fields

    private readonly ILrcProvider sourceProvider;

    #endregion

    #region Constructors

    public LRCFileViewModel(LRCFile model, LRCProviders lRcProvider) : base(model)
    {
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

    private TimeSpan? lastTimestamp;
    private TimeSpan? nextTimestamp;
    private object batton = new object();

    public void SetActualLine(TimeSpan timeSpan)
    {
      lock (batton)
      {
        if (lastTimestamp == null ||
            (lastTimestamp <= timeSpan &&
             nextTimestamp <= timeSpan) || (lastTimestamp > timeSpan))
        {
          if (ActualLine != null)
          {
            ActualLine.IsActual = false;
          }

          var newLine = Lines.Where(x => x.Model.Timestamp <= timeSpan).OrderByDescending(x => x.Model.Timestamp).FirstOrDefault();

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

                if (nextTimestampIndex < Lines.Count)
                {
                  nextTimestamp = Lines[nextTimestampIndex].Model.Timestamp;
                }
                else
                {
                  nextTimestamp = null;
                  break;
                }


              } while (nextTimestamp == oldTimestamp);


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
  }
}