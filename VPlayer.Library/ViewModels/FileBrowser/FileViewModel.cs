using System;
using System.IO;
using VCore.Annotations;

namespace VPlayer.Library.ViewModels.FileBrowser
{
  public enum FileType
  {
    Video,
    Sound,
    Image,
    TextFile,
    CompressFile,
    Subtitles,
    Other
  }

  public static class ExtentionHelper
  {
    public static FileType GetFileType(this string extention)
    {
      switch (extention)
      {
        case ".mp4":
        case ".mkv":
        case ".avi":
        {
          return FileType.Video;
        }
        case ".flac":
        case ".mp3":
        {
          return FileType.Sound;
          }
        case ".jpg":
        {
          return FileType.Image;
        }
        case ".txt":
        {
          return FileType.TextFile;
        }
        case ".srt":
        {
          return FileType.Subtitles;
        }
        case ".zip":
        case ".rar":
        {
          return FileType.CompressFile;
        }
        default:
        {
          return FileType.Other;
        }
      }
    }

  }

  public class FileViewModel : WindowsItem<FileInfo>
  {

    public FileViewModel([NotNull] FileInfo fileInfo) : base(fileInfo)
    {
      var extention = System.IO.Path.GetExtension(fileInfo.FullName);

      FileType = extention.GetFileType();
    }



    #region FileType

    private FileType fileType;

    public FileType FileType
    {
      get { return fileType; }
      set
      {
        if (value != fileType)
        {
          fileType = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

  }
}