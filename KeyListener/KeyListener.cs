using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace KeyListener
{
  public static class KeyListener
  {
    #region Fields

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;

    private static IntPtr _hookID = IntPtr.Zero;

    private static LowLevelKeyboardProc _proc;

    #endregion Fields

    #region Constructors

    static KeyListener()
    {
      _proc = HookCallback;
      HookKeyboard();
    }

    #endregion Constructors

    #region Delegates

    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    #endregion Delegates

    #region Events

    public static event EventHandler<KeyPressedArgs> OnKeyPressed;

    #endregion Events

    #region Methods

    public static void HookKeyboard()
    {
      _hookID = SetHook(_proc);
    }

    public static void UnHookKeyboard()
    {
      UnhookWindowsHookEx(_hookID);
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(
      IntPtr hhk,
      int nCode,
      IntPtr wParam,
      IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
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

      return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
      using (Process curProcess = Process.GetCurrentProcess())
      using (ProcessModule curModule = curProcess.MainModule)
      {
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
      }
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
      int idHook,
      LowLevelKeyboardProc lpfn,
      IntPtr hMod,
      uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    #endregion Methods
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
}