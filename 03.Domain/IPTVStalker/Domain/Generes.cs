using System;
using System.Collections.Generic;
using System.Text;

namespace IPTVStalker.Domain
{
  public class Genere
  {
    public string id { get; set; }
    public string title { get; set; }
    public string alias { get; set; }
    public bool active_sub { get; set; }
    public int censored { get; set; }
    public string modified { get; set; }
    public int? number { get; set; }
  }

  public class GeneresResponse
  {
    public List<Genere> js { get; set; }
  }


}
