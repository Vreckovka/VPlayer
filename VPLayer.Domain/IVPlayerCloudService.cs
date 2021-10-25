using System.Collections.Generic;
using System.Threading.Tasks;
using PCloudClient;
using VCore.Standard;
using VCore.WPF.ViewModels.WindowsFiles;

namespace VPLayer.Domain
{
  public interface IVPlayerCloudService : IPCloudService
  {
    AsyncProcess<IEnumerable<FileInfo>> GetItemSources(IEnumerable<FileInfo> fileInfos);
  }
}