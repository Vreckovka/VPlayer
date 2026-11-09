# Native ASS subtitle styling

The VPlayer VLC module decodes ASS/SSA packets from the existing subtitle track into text subpictures. VLC's regular text renderer applies VPlayer's font, size, color, and background settings. The decoder keeps the timestamps and track ID and ignores inline styling, positioning, and vector drawings. It does not create files or add tracks. Other subtitle codecs continue to use VLC's existing decoders.

The bundled x64 module is built against the Windows SDK in the `VideoLAN.LibVLC.Windows` **3.0.20** NuGet package. VPlayer copies it into its private VLC plugin directory. It does not replace or change any VLC installation on the computer.

To rebuild the module, restore the VPlayer NuGet packages and run `Build.ps1 -ClangPath <llvm-mingw x64 clang.exe>`. The script runs six packet tests before producing the DLL. The bundled binary was built with [LLVM-MinGW 20261104](https://github.com/mstorsjo/llvm-mingw/releases/tag/20261104), UCRT, x64. Rebuild and verify playback against the matching SDK if the bundled VLC version changes.

`substext.h` is unmodified from [VideoLAN VLC 3.0.20](https://github.com/videolan/vlc/blob/3.0.20/modules/codec/substext.h). The decoder and its source are licensed under LGPL-2.1-or-later; see [COPYING.LIB](COPYING.LIB).
