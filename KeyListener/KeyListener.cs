using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;


namespace Listener
{
  public enum MouseMessages
  {
    WM_LBUTTONDOWN = 0x0201,
    WM_LBUTTONUP = 0x0202,
    WM_MOUSEMOVE = 0x0200,
    WM_MOUSEWHEEL = 0x020A,
    WM_RBUTTONDOWN = 0x0204,
    WM_RBUTTONUP = 0x0205
  }

  public class KeyListener : IDisposable
  {
    #region Fields

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WH_MOUSE_LL = 14;

    private IntPtr _keyboardHookID = IntPtr.Zero;
    private IntPtr _mouseHookID = IntPtr.Zero;

    #region Delegates

    public delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

    #endregion Delegates

    private LowLevelProc _procKeyboard;
    private LowLevelProc _procMouse;

    #endregion Fields

    #region Constructors

    private Dispatcher myDispatcher;
    private Thread thread;
    private ManualResetEvent dispatcherReadyEvent;

    public KeyListener()
    {
      ManualResetEvent dispatcherReadyEvent = new ManualResetEvent(false);

      thread = new Thread((() =>
      {
        myDispatcher = Dispatcher.CurrentDispatcher;
        dispatcherReadyEvent.Set();
        Dispatcher.Run();
      }));

      thread.Start();

      dispatcherReadyEvent.WaitOne();

#if RELEASE
      myDispatcher.Invoke(() =>
      {
        _procKeyboard = HookCallbackKeyboard;
        _procMouse = HookCallbackMouse;

        HookKeyboard();
        HookMouse();
      });
#endif
    }

    #endregion Constructors


    #region Events

    public event EventHandler<KeyPressedArgs> OnKeyPressed;

    public event EventHandler<MouseEventArgs> OnMouseEvent;

    #endregion Events

    #region Methods

    #region HookKeyboard

    public void HookKeyboard()
    {
      _keyboardHookID = SetHookForKeyboard(_procKeyboard);

    }

    #endregion

    #region HookMouse

    public void HookMouse()
    {
      if (_mouseHookID == IntPtr.Zero)
      {
        _mouseHookID = SetHookForMouse(_procMouse);
      }
    }

    #endregion

    public void UnHookKeyboard()
    {
      UnhookWindowsHookEx(_keyboardHookID);

      _keyboardHookID = IntPtr.Zero;
    }

    public void UnHookMouse()
    {
      UnhookWindowsHookEx(_mouseHookID);

      _mouseHookID = IntPtr.Zero;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(
      IntPtr hhk,
      int nCode,
      IntPtr wParam,
      IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    #region HookCallbackKeyboard

    private IntPtr HookCallbackKeyboard(int nCode, IntPtr wParam, IntPtr lParam)
    {
      if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
      {
        int vkCode = Marshal.ReadInt32(lParam);

        if (OnKeyPressed != null)
        {
          OnKeyPressed(null, new KeyPressedArgs(KeyInterop.KeyFromVirtualKey(vkCode)));
        }

        //Return a dummy value to trap the keystroke
        //return (System.IntPtr)1;
      }

      return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
    }

    #endregion

    #region HookCallbackMouse

    private IntPtr HookCallbackMouse(int nCode, IntPtr wParam, IntPtr lParam)
    {
      if (nCode >= 0 &&
        wParam == (IntPtr)MouseMessages.WM_LBUTTONDOWN ||
        wParam == (IntPtr)MouseMessages.WM_LBUTTONUP ||
        wParam == (IntPtr)MouseMessages.WM_MOUSEMOVE ||
        wParam == (IntPtr)MouseMessages.WM_MOUSEWHEEL ||
        wParam == (IntPtr)MouseMessages.WM_RBUTTONDOWN ||
        wParam == (IntPtr)MouseMessages.WM_RBUTTONUP)
      {
        if (OnMouseEvent != null)
        {
          var eventType = (MouseMessages)wParam;

          var mouseEvent = new MouseEventArgs()
          {
            Event = eventType
          };

          unsafe
          {
            if (eventType == MouseMessages.WM_MOUSEWHEEL)
            {
              MSLLHOOKSTRUCT* mouselparam = (MSLLHOOKSTRUCT*)lParam;

              short delta = NativeMethods.HighWord(mouselparam->mouseData);

              mouseEvent.EventData = (int)delta;
            }
          }

          OnMouseEvent(null, mouseEvent);
        }
      }

      return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
    }

    #endregion

    #region SetHookForKeyboard

    private static IntPtr SetHookForKeyboard(LowLevelProc proc)
    {
      using (Process curProcess = Process.GetCurrentProcess())
      using (ProcessModule curModule = curProcess.MainModule)
      {
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
      }
    }

    #endregion

    #region SetHookForMouse

    private static IntPtr SetHookForMouse(LowLevelProc proc)
    {
      using (Process curProcess = Process.GetCurrentProcess())
      using (ProcessModule curModule = curProcess.MainModule)
      {
        return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
      }
    }

    #endregion

    #region SetWindowsHookEx

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
      int idHook,
      LowLevelProc lpfn,
      IntPtr hMod,
      uint dwThreadId);

    #endregion

    #region UnhookWindowsHookEx

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    #endregion


    #endregion

    #region Structs

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
      public int x;
      public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
      public POINT pt;
      public uint mouseData;
      public uint flags;
      public uint time;
      public IntPtr dwExtraInfo;
    }

    #endregion

    public void Dispose()
    {
      UnHookMouse();
      UnHookKeyboard();
      thread?.Abort();
      myDispatcher?.InvokeShutdown();
      dispatcherReadyEvent?.Dispose();
    }
  }

  public class KeyPressedArgs : EventArgs
  {
    #region Constructors

    public KeyPressedArgs(Key key)
    {
      KeyPressed = key;
    }

    #endregion Constructors

    #region Properties

    public Key KeyPressed { get; private set; }

    #endregion Properties
  }

  public class MouseEventArgs : EventArgs
  {
    public MouseMessages Event { get; set; }

    public object EventData { get; set; }
  }

  internal static class NativeMethods
  {
    public static short LowWord(uint input)
    {
      return (short)(input & 0xffff);
    }

    public static short HighWord(uint input)
    {
      return (short)(input >> 16);
    }
  }
}