using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCore.WPF.ViewModels.WindowsFiles;
using VPLayer.Domain;
using VPlayer.PCloud;

namespace VPlayer.Providers
{
  public class VPlayerCloudService : PCloudCloudService, IVPlayerCloudService
  {
    public VPlayerCloudService(string filePath) : base(filePath)
    {
    }

    public async Task<IEnumerable<FileInfo>> GetItemSources(IEnumerable<FileInfo> fileInfos)
    {
      var list = fileInfos.ToList();

      var links = await GetFileLinks(list.Select(x => long.Parse(x.Indentificator)));

      foreach (var item in links)
      {
        var info = list.Single(x => x.Indentificator == item.Key.ToString());

        info.Source = item.Value;
      }

      return list;
    }
  }
}