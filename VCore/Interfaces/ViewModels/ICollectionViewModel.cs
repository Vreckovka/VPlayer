using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VCore.Interfaces.ViewModels
{
  public interface ICollectionViewModel<TViewModel, TModel>
  {
    #region Methods

    Task<ICollection<TViewModel>> GetViewModelsAsync(IQueryable<TModel> optionalQuery = null);

    #endregion Methods
  }
}