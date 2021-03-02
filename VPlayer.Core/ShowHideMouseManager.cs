using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Timers;
using System.Windows;
using System.Windows.Input;

namespace VPlayer.WindowsPlayer.Behaviors
{
  public static class ShowHideMouseManager
  {
    private static Timer cursorTimer;
    private static ElapsedEventHandler hideCursorDelegate;
    private static ReplaySubject<Unit> resetMouseSubject = new ReplaySubject<Unit>(1);
    private static ReplaySubject<Unit> hideMouseSubject = new ReplaySubject<Unit>(1);

    static ShowHideMouseManager()
    {
      cursorTimer = new Timer(1500);
      cursorTimer.AutoReset = false;

      hideCursorDelegate = (s, e) =>
      {
        if (IsFullscreen)
        {
          hideMouseSubject.OnNext(Unit.Default);
          SafeOverrideCursor(Cursors.None);
          IsMouseHidden = true;
        }
      };

      cursorTimer.Elapsed += hideCursorDelegate;
    }

    #region OnResetMouse

    public static IObservable<Unit> OnResetMouse
    {
      get
      {
        return resetMouseSubject.AsObservable();
      }
    }

    #endregion

    #region OnHideMouse

    public static IObservable<Unit> OnHideMouse
    {
      get
      {
        return hideMouseSubject.AsObservable();
      }
    }

    #endregion

    #region IsFullscreen

    private static bool isFullscreen;

    public static bool IsFullscreen
    {
      get { return isFullscreen; }
      set
      {
        if (value != isFullscreen)
        {
          isFullscreen = value;

          if (isFullscreen)
          {
            cursorTimer.Stop();
            cursorTimer.Start();
          }

          ResetMouse();
        }
      }
    }

    #endregion

    public static bool IsMouseHidden { get; set; }

    #region ResetMouse

    public static void ResetMouse()
    {
      if (IsMouseHidden)
      {
        cursorTimer.Stop();

        Mouse.OverrideCursor = null; //Show cursor
        resetMouseSubject.OnNext(Unit.Default);
        IsMouseHidden = false;

        cursorTimer.Start();
      }

    }

    #endregion

    #region SafeOverrideCursor

    private static void SafeOverrideCursor(Cursor cursor)
    {
      Application.Current.Dispatcher.Invoke(new Action(() =>
      {
        Mouse.OverrideCursor = cursor;
      }));
    }

    #endregion

  }
}