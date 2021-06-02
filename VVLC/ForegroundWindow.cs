using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LibVLCSharp.Shared;

namespace VPlayer.WindowsPlayer.Vlc
{
  internal class ForegroundWindow : Window
  {
    #region Fields

    Window? _wndhost;
    readonly FrameworkElement _bckgnd;
    readonly Point _zeroPoint = new Point(0, 0);
    private readonly Grid _grid = new Grid();
    private Window overlayWindow = new Window();

    #endregion

    #region Constructors

    internal ForegroundWindow(FrameworkElement frameworkElement)
    {
      Title = "LibVLCSharp.WPF";
      Height = 300;
      Width = 300;
      WindowStyle = WindowStyle.None;
      Background = Brushes.Black;
      ResizeMode = ResizeMode.NoResize;
      AllowsTransparency = true;
      ShowInTaskbar = false;
      Style = null;

      _bckgnd = frameworkElement;

      DataContext = _bckgnd.DataContext;

      overlayWindow = new Window();
      overlayWindow.WindowStyle = WindowStyle.None;
      overlayWindow.ResizeMode = ResizeMode.NoResize;
      overlayWindow.AllowsTransparency = true;
      overlayWindow.ShowInTaskbar = false;
      overlayWindow.Style = null;
      overlayWindow.Background = Brushes.Transparent;

      overlayWindow.Content = _grid;
      overlayWindow.DataContext = _bckgnd.DataContext;
      overlayWindow.MouseLeftButtonDown += OverlayWindow_GotFocus;

      _bckgnd.DataContextChanged += Background_DataContextChanged;
      _bckgnd.Loaded += Background_Loaded;
      _bckgnd.Unloaded += Background_Unloaded;


      IsVisibleChanged += ForegroundWindow_IsVisibleChanged;
    }


    #endregion

    #region ForegroundWindow_IsVisibleChanged

    private void ForegroundWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      if (!_bckgnd.IsLoaded && IsVisible)
      {
        Height = 0;
        Width = 0;
      }
    } 

    #endregion

    #region OverlayWindow_GotFocus

    private void OverlayWindow_GotFocus(object sender, RoutedEventArgs e)
    {
      _wndhost?.Focus();
    }

    #endregion

    #region OnStateChanged

    protected override void OnStateChanged(EventArgs e)
    {
      base.OnStateChanged(e);

      overlayWindow.WindowState = WindowState;

      _wndhost?.Focus();
    }

    #endregion

    #region OverlayContent

    UIElement? _overlayContent;
    internal UIElement? OverlayContent
    {
      get => _overlayContent;
      set
      {
        _overlayContent = value;
        _grid.Children.Clear();
        if (_overlayContent != null)
        {
          _grid.Children.Add(_overlayContent);
        }
      }
    }

    #endregion

    #region Background_DataContextChanged

    void Background_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      DataContext = e.NewValue;
      overlayWindow.DataContext = DataContext;
    }

    #endregion

    #region Background_Unloaded

    void Background_Unloaded(object sender, RoutedEventArgs e)
    {
      _bckgnd.SizeChanged -= Wndhost_SizeChanged;

      if (_wndhost != null)
      {
        _wndhost.Closing -= Wndhost_Closing;
        _wndhost.LocationChanged -= Wndhost_LocationChanged;
        _wndhost.StateChanged -= Wndhost_StateChanged;
        _wndhost.SizeChanged -= Wndhost_SizeChanged;
      }

      Hide();
      overlayWindow.Hide();
    }

    #endregion

    #region Background_Loaded

    void Background_Loaded(object sender, RoutedEventArgs e)
    {
      if (_wndhost == null)
      {
        _wndhost = GetWindow(_bckgnd);
      }

      Trace.Assert(_wndhost != null);
      if (_wndhost == null)
      {
        return;
      }

      Owner = _wndhost;
      SetWindowInPlace();

      _wndhost.Closing += Wndhost_Closing;
      _bckgnd.SizeChanged += Wndhost_SizeChanged;
      _bckgnd.DataContextChanged += _bckgnd_DataContextChanged;
      _wndhost.LocationChanged += Wndhost_LocationChanged;
      _wndhost.SizeChanged += Wndhost_SizeChanged;
      _wndhost.StateChanged += Wndhost_StateChanged;

      try
      {
        SetWindowInPlace();
      }
      catch (Exception ex)
      {
        Hide();
        throw new VLCException("Unable to create WPF Window in VideoView.", ex);
      }
    }

    private void _bckgnd_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
    }


    #endregion

    #region Wndhost_StateChanged

    private void Wndhost_StateChanged(object sender, EventArgs e)
    {
     // if (_wndhost.WindowState != WindowState.Minimized)
      {
        SetWindowInPlace();
      }
    }

    #endregion

    #region SetWindowInPlace

    private void SetWindowInPlace()
    {
      var locationFromScreen = _bckgnd.PointToScreen(_zeroPoint);
      var source = PresentationSource.FromVisual(_wndhost);
      var targetPoints = source.CompositionTarget.TransformFromDevice.Transform(locationFromScreen);
      Left = targetPoints.X;
      Top = targetPoints.Y;
      var size = new Point(_bckgnd.ActualWidth, _bckgnd.ActualHeight);
      Height = size.Y;
      Width = size.X;
      Show();

      overlayWindow.Left = Left;
      overlayWindow.Top = Top;
      overlayWindow.Height = Height;
      overlayWindow.Width = Width;

      overlayWindow.Show();
      overlayWindow.Owner = this;

      _wndhost.Focus();

    }

    #endregion

    #region Wndhost_LocationChanged

    void Wndhost_LocationChanged(object? sender, EventArgs e)
    {
      if (overlayWindow.WindowState == WindowState.Maximized)
      {
        overlayWindow.WindowState = WindowState.Normal;
        WindowState = WindowState.Normal;
      }

      var locationFromScreen = _bckgnd.PointToScreen(_zeroPoint);
      var source = PresentationSource.FromVisual(_wndhost);
      var targetPoints = source.CompositionTarget.TransformFromDevice.Transform(locationFromScreen);
      Left = targetPoints.X;
      Top = targetPoints.Y;

      overlayWindow.Left = Left;
      overlayWindow.Top = Top;

    }

    #endregion

    #region Wndhost_SizeChanged

    void Wndhost_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      var source = PresentationSource.FromVisual(_wndhost);
      if (source == null)
      {
        return;
      }

      var locationFromScreen = _bckgnd.PointToScreen(_zeroPoint);
      var targetPoints = source.CompositionTarget.TransformFromDevice.Transform(locationFromScreen);
      Left = targetPoints.X;
      Top = targetPoints.Y;
      var size = new Point(_bckgnd.ActualWidth, _bckgnd.ActualHeight);
      Height = size.Y;
      Width = size.X;


      overlayWindow.Left = Left;
      overlayWindow.Top = Top;
      overlayWindow.Height = Height;
      overlayWindow.Width = Width;
    }

    #endregion

    #region Wndhost_Closing

    void Wndhost_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      Close();
      overlayWindow.Content = null;
      overlayWindow.Close();

      _bckgnd.DataContextChanged -= Background_DataContextChanged;
      _bckgnd.Loaded -= Background_Loaded;
      _bckgnd.Unloaded -= Background_Unloaded;
      IsVisibleChanged -= ForegroundWindow_IsVisibleChanged;
    }

    #endregion

  
  }
}
