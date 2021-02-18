using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace VPlayer.AudioStorage.Repositories
{
  public abstract class GenericRepository<TContext, TEntity> : IGenericRepository<TEntity> where TEntity : class where TContext : DbContext, new()
  {
    #region Fields

    private TContext context = new TContext();

    #endregion Fields

    #region Contructors

    public GenericRepository(TContext context)
    {
      if (context != null) this.context = context;
    }

    #endregion

    #region Properties

    #region Context

    public TContext Context
    {
      get { return context; }
      set { context = value; }
    }

    #endregion

    #region Entities

    public DbSet<TEntity> Entities
    {
      get
      {
        context = new TContext();
        return context.Set<TEntity>(); 

      }
    }

    #endregion

    #endregion 

    #region Methods

    public virtual void Add(TEntity entity)
    {
      context.Set<TEntity>().Add(entity);
    }

    public virtual void Delete(TEntity entity)
    {
     context.Set<TEntity>().Remove(entity);
    }

    public virtual void Edit(TEntity entity)
    {
      context.Entry(entity).State = EntityState.Modified;
    }

    public IQueryable<TEntity> FindBy(System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate)
    {
      IQueryable<TEntity> query = context.Set<TEntity>().Where(predicate);
      return query;
    }

    public virtual IQueryable<TEntity> GetAll()
    {
      IQueryable<TEntity> query = context.Set<TEntity>();
      return query;
    }

    public virtual void Save()
    {
      context.SaveChanges();
    }

    #endregion Methods
  }
}