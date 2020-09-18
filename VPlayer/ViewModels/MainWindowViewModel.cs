using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using VCore.Factories;
using VCore.ViewModels;
using VCore.ViewModels.Navigation;
using VPlayer.Player.ViewModels;
using VPlayer.WindowsPlayer.ViewModels;

namespace VPlayer.ViewModels
{

  public class MainWindowViewModel : BaseMainWindowViewModel
  {
    #region Fields

    private readonly IViewModelsFactory viewModelsFactory;

    #endregion

    #region Constructors

    public MainWindowViewModel(IViewModelsFactory viewModelsFactory)
    {
      this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
    }

    #endregion

    #region Properties

    public NavigationViewModel NavigationViewModel { get; set; } = new NavigationViewModel();
    public override string Title => "VPlayer";

    #region IsWindows

    private bool isWindows;
    public bool IsWindows
    {
      get { return isWindows; }
      set
      {
        if (value != isWindows)
        {
          isWindows = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #endregion

    #region Methods

    #region Initilize

    public override void Initialize()
    {
      base.Initialize();

      var windowsPlayer = viewModelsFactory.Create<WindowsViewModel>();
      windowsPlayer.IsActive = true;
      isWindows = true;
      NavigationViewModel.Items.Add(windowsPlayer);


      var player = viewModelsFactory.Create<PlayerViewModel>();
      player.IsActive = true;

#if DEBUG
      WinConsole.Initialize();
#endif

    }

    #endregion

    #endregion

    #region MyRegion

    public override void Dispose()
    {
      base.Dispose();

      foreach (var item in NavigationViewModel.Items)
      {
        item?.Dispose();
      }
    }

    #endregion

  }

  static class WinConsole
  {
    public static void Initialize(bool alwaysCreateNewConsole = true)
    {
      bool consoleAttached = true;
      if (alwaysCreateNewConsole
          || (AttachConsole(ATTACH_PARRENT) == 0
          && Marshal.GetLastWin32Error() != ERROR_ACCESS_DENIED))
      {
        consoleAttached = AllocConsole() != 0;
      }

      if (consoleAttached)
      {
        InitializeOutStream();
        InitializeInStream();
      }
    }

    private static void InitializeOutStream()
    {
      var fs = CreateFileStream("CONOUT$", GENERIC_WRITE, FILE_SHARE_WRITE, FileAccess.Write);
      if (fs != null)
      {
        var writer = new StreamWriter(fs) { AutoFlush = true };

        Console.SetOut(writer);
        Console.SetError(writer);
      }
    }

    private static void InitializeInStream()
    {
      var fs = CreateFileStream("CONIN$", GENERIC_READ, FILE_SHARE_READ, FileAccess.Read);
      if (fs != null)
      {
        Console.SetIn(new StreamReader(fs));
      }
    }

    private static FileStream CreateFileStream(string name, uint win32DesiredAccess, uint win32ShareMode,
                            FileAccess dotNetFileAccess)
    {
      var file = new SafeFileHandle(CreateFileW(name, win32DesiredAccess, win32ShareMode, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero), true);
      if (!file.IsInvalid)
      {
        var fs = new FileStream(file, dotNetFileAccess);
        return fs;
      }
      return null;
    }

    #region Win API Functions and Constants
    [DllImport("kernel32.dll",
        EntryPoint = "AllocConsole",
        SetLastError = true,
        CharSet = CharSet.Auto,
        CallingConvention = CallingConvention.StdCall)]
    private static extern int AllocConsole();

    [DllImport("kernel32.dll",
        EntryPoint = "AttachConsole",
        SetLastError = true,
        CharSet = CharSet.Auto,
        CallingConvention = CallingConvention.StdCall)]
    private static extern UInt32 AttachConsole(UInt32 dwProcessId);

    [DllImport("kernel32.dll",
        EntryPoint = "CreateFileW",
        SetLastError = true,
        CharSet = CharSet.Auto,
        CallingConvention = CallingConvention.StdCall)]
    private static extern IntPtr CreateFileW(
          string lpFileName,
          UInt32 dwDesiredAccess,
          UInt32 dwShareMode,
          IntPtr lpSecurityAttributes,
          UInt32 dwCreationDisposition,
          UInt32 dwFlagsAndAttributes,
          IntPtr hTemplateFile
        );

    private const UInt32 GENERIC_WRITE = 0x40000000;
    private const UInt32 GENERIC_READ = 0x80000000;
    private const UInt32 FILE_SHARE_READ = 0x00000001;
    private const UInt32 FILE_SHARE_WRITE = 0x00000002;
    private const UInt32 OPEN_EXISTING = 0x00000003;
    private const UInt32 FILE_ATTRIBUTE_NORMAL = 0x80;
    private const UInt32 ERROR_ACCESS_DENIED = 5;

    private const UInt32 ATTACH_PARRENT = 0xFFFFFFFF;

    #endregion
  }
}
