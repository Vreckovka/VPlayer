using System;
using System.Linq;
using System.Linq.Expressions;

namespace VPlayer.AudioStorage.Repositories
{
  public interface IGenericRepository<T> where T : class
  {
    #region Methods

    void Add(T entity);

    void Delete(T entity);

    void Edit(T entity);

    IQueryable<T> FindBy(Expression<Func<T, bool>> predicate);

    IQueryable<T> GetAll();

    void Save();

    #endregion Methods
  }
}