using System.Collections.Generic;
using System.Linq;
using IPTVStalker;

public class IptvStalkerServiceProvider : IIptvStalkerServiceProvider
{
  private Dictionary<ConnectionProperties, IPTVStalkerService> services = new Dictionary<ConnectionProperties, IPTVStalkerService>();

  public IPTVStalkerService GetStalkerService(string url, string macAddress)
  {
    var serviceProperties = services.Keys.SingleOrDefault(x => x.MAC == macAddress && x.Server == url);

    if(serviceProperties == null)
    {
      var connectionProperties = new ConnectionProperties()
      {
        TimeZone = "GMT",
        MAC = macAddress,
        Server = url,
      };

      var serviceStalker = new IPTVStalkerService(connectionProperties);

      services.Add(connectionProperties, serviceStalker);

      return serviceStalker;
    }

    return services[serviceProperties];

  }

}