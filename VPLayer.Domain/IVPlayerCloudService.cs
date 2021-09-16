using System.Collections.Generic;
using System.Threading.Tasks;
using VCore.WPF.ViewModels.WindowsFiles;
using VPLayer.Domain.Contracts.CloudService.Providers;

namespace VPLayer.Domain
{
  public interface IVPlayerCloudService : ICloudService
  {
    Task<IEnumerable<FileInfo>> GetItemSources(IEnumerable<FileInfo> fileInfos);
  }
}