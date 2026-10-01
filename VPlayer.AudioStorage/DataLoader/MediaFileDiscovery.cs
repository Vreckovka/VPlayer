using System;
using System.Collections.Generic;
using System.IO;

namespace VPlayer.AudioStorage.DataLoader
{
  public static class MediaFileDiscovery
  {
    public static IEnumerable<FileInfo> EnumerateFiles(string directoryPath, IEnumerable<string> fileTypes, bool recursive)
    {
      if (directoryPath == null) throw new ArgumentNullException(nameof(directoryPath));
      if (fileTypes == null) throw new ArgumentNullException(nameof(fileTypes));
      var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      foreach (var pattern in fileTypes)
        extensions.Add(pattern.TrimStart('*'));
      return EnumerateDirectory(new DirectoryInfo(directoryPath), extensions, recursive);
    }

    private static IEnumerable<FileInfo> EnumerateDirectory(DirectoryInfo directory, HashSet<string> extensions, bool recursive)
    {
      var options = new EnumerationOptions
      {
        RecurseSubdirectories = false,
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.ReparsePoint
      };
      // Preserve child-before-parent traversal without copying every subtree into intermediate lists.
      if (recursive)
        foreach (var child in directory.EnumerateDirectories("*", options))
          foreach (var file in EnumerateDirectory(child, extensions, true))
            yield return file;

      foreach (var file in directory.EnumerateFiles("*", options))
        if (extensions.Contains(file.Extension))
          yield return file;
    }
  }
}
