namespace PCloudClient.Domain
{
  public class PCloudResponse<TData>
  {
    public string result { get; set; }
    public TData metadata { get; set; }
  }

 
  public class Stats
  {
    public string name { get; set; }
    public string created { get; set; }
    public string album { get; set; }
    public string thumb { get; set; }
    public string modified { get; set; }
    public string artist { get; set; }
    public string genre { get; set; }
    public string isfolder { get; set; }
    public string fileid { get; set; }
    public string trackno { get; set; }
    public string isshared { get; set; }
    public string hash { get; set; }
    public string comments { get; set; }
    public string category { get; set; }
    public string id { get; set; }
    public string title { get; set; }
    public string ismine { get; set; }
    public string size { get; set; }
    public string parentfolderid { get; set; }
    public string contenttype { get; set; }
    public string icon { get; set; }
  }
}