using System;
using System.Collections.Generic;
using System.Text;

namespace VPlayer.AudioStorage.DomainClasses.FolderStructure
{
  public enum FileBrowserType
  {
    Local,
    Cloud
  }

  public class ItemBookmark : DomainEntity
  {
    public string Identificator { get; set; }
    public string Path { get; set; }
    public FileBrowserType FileBrowserType { get; set; }
  }
}
