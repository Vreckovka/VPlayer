using IPTVStalker;

public interface IIptvStalkerServiceProvider
{
  IPTVStalkerService GetStalkerService(string url, string macAddress);
}