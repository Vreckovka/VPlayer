using System;
using PropertyChanged;

namespace VPlayer.WebPlayer.Models
{
  public class InternetPlayer
  {
    public Uri Uri { get; set; }
    public string Title { get; set; }
    public string HTMLElementClass { get; set; }
  }
}
