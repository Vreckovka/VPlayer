using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace VVLC
{
  internal static class OwnedWindowOrder
  {
    const uint KeepGeometryAndActivation=0x0001|0x0002|0x0010;
    const uint KeepOwnerOrder=0x0200;
    [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr window,uint flags);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")] static extern IntPtr GetWindowLongPtr(IntPtr window,int index);
    [DllImport("user32.dll",EntryPoint="GetWindowLongW")] static extern int GetWindowLong32(IntPtr window,int index);
    [DllImport("user32.dll")] static extern bool IsWindow(IntPtr window);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);

    internal static void Show(Window window)
    {
      // A surface shown before its host loads would become an independent popup.
      if(window.IsVisible || window.Owner==null) return;
      var root=window.Owner;
      while(root.Owner!=null) root=root.Owner;
      var rootHandle=new WindowInteropHelper(root).Handle;
      var foreground=GetForegroundWindow();
      bool preserve=rootHandle!=IntPtr.Zero && !root.IsActive && !root.Topmost &&
        GetAncestor(foreground,3)!=rootHandle;
      bool foregroundIsTopmost=foreground!=IntPtr.Zero &&
        ((IntPtr.Size==8?GetWindowLongPtr(foreground,-20).ToInt64():GetWindowLong32(foreground,-20)) & 8)!=0;

      window.Show();

      if(preserve && !root.IsActive && !root.Topmost && GetForegroundWindow()==foreground && IsWindow(foreground))
      {
        // ShowActivated=false prevents activation but Show can still raise the
        // owner group. Keep it below the user's foreground window without focus
        // or geometry changes. Avoid promotion when the foreground is topmost.
        var insertAfter=foregroundIsTopmost?IntPtr.Zero:foreground;
        SetWindowPos(rootHandle,insertAfter,0,0,0,0,KeepGeometryAndActivation);
        var chain=new Stack<Window>();
        for(var item=window;item!=root;item=item.Owner) chain.Push(item);
        while(chain.Count>0)
          SetWindowPos(new WindowInteropHelper(chain.Pop()).Handle,insertAfter,0,0,0,0,KeepGeometryAndActivation|KeepOwnerOrder);
      }
    }
  }
}
