using System;
using VPlayer.Core.Providers;

namespace VPlayer
{
  public class VPlayerBasicInformationProvider : IBasicInformationProvider
  {
    public string GetBuildVersion(string stringFormat = "dd.MM.yyyy")
    {
      Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

      DateTime buildDate = new DateTime(2000, 1, 1).AddDays(version.Build).AddSeconds(version.Revision * 2);

      return $"{version} ({buildDate.ToString(stringFormat)})";
    }
  }
}
