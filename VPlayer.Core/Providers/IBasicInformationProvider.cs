namespace VPlayer.Core.Providers
{
  public interface IBasicInformationProvider
  {
    string GetBuildVersion(string stringFormat = "dd.MM.yyyy");
  }
}