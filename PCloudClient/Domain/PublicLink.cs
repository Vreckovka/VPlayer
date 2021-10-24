using System;
using System.Collections.Generic;
using System.Text;

namespace PCloudClient.Domain
{
  public class PublicLink
  {
    public string Link { get; set; }
    public long Exipires { get; set; }
    public string[] Hosts { get; set; }

    public DateTime ExpiresDate { get; set; }
  }
}
