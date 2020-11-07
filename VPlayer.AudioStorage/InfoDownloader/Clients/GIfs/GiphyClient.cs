using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using unirest_net.http;
using VPlayer.AudioStorage.DomainClasses;

namespace VPlayer.AudioStorage.InfoDownloader.Clients.GIfs
{
  public class GiphyClient
  {
    public async Task<Gif> GetRandomGif(string tag = "random")
    {
      try
      {
        var apiURL = $"https://api.giphy.com/v1/gifs/random?api_key=iA3jx8eEs3I13Jj9VvF3MGGvQ1rmGiXe&tag={tag}&rating=g";
        HttpResponse<string> response = await Task.Run(() => Unirest.get(apiURL).asJson<string>());

        if (response.Code == 200)
        {
          dynamic jsonObject = JObject.Parse(response.Body);

          var data = jsonObject.data;
          var images = data.images;
          var largeImage = images.downsized_large;
          var url = largeImage.url;

          return new Gif()
          {
            Url = url
          };
        }

        return null;
      }
      catch (Exception ex)
      {
        return null;
      }
    }
  }
}
