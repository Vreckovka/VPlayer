using System.Collections.Generic;
using System.Threading.Tasks;

namespace VCore.Interfaces.ViewModels
{
  public interface ICollectionViewModel<T>
  {
    #region Methods

    Task<ICollection<T>> GetViewModelsAsync();

    #endregion Methods
  }
}