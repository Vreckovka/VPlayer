namespace VPlayer.AudioStorage.DomainClasses
{
  public interface IUpdateable<TEntity> 
  {
    void Update(TEntity other);
  }
}