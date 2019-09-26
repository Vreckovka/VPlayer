using System.Collections.Generic;

namespace VCore.Interfaces.ViewModels
{
  public interface ICollectionViewModel<T>
  {
    ICollection<T> ViewModels { get; }
  }
}