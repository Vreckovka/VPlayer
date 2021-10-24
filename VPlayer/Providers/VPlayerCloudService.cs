
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Logger;
using VCore.Standard;
using VCore.WPF.ViewModels.WindowsFiles;
using VPLayer.Domain;
using VPlayer.PCloud;

namespace VPlayer.Providers
{
  public class VPlayerCloudService : PCloudCloudService, IVPlayerCloudService
  {
    public VPlayerCloudService(string filePath, ILogger logger) : base(filePath, logger)
    {
    }

    public AsyncProcess<IEnumerable<FileInfo>> GetItemSources(IEnumerable<FileInfo> fileInfos)
    {
      var process = new AsyncProcess<IEnumerable<FileInfo>>();
      var list = fileInfos.ToList();
      var linksProcess = GetFileLinks(list.Select(x => long.Parse(x.Indentificator)));

      process.InternalProcessesCount = linksProcess.InternalProcessesCount;

      process.Process = Task.Run(async () =>
      {
        linksProcess.OnInternalProcessedCountChanged.Subscribe((x) =>
        {
          process.ProcessedCount = x;
        });

        var links = await linksProcess.Process;

        foreach (var item in links)
        {
          var info = list.Single(x => x.Indentificator == item.Key.ToString());

          info.Source = item.Value.Link;
        }

        return list.AsEnumerable();
      });

      return process;
    }
  }
}