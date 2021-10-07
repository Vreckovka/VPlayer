using System.Collections.Generic;
using System.Threading.Tasks;
using VCore.Standard;
using VCore.WPF.ViewModels.WindowsFiles;
using VPLayer.Domain.Contracts.CloudService.Providers;

namespace VPLayer.Domain
{
  public interface IVPlayerCloudService : ICloudService
  {
    AsyncProcess<IEnumerable<FileInfo>> GetItemSources(IEnumerable<FileInfo> fileInfos);
  }
}