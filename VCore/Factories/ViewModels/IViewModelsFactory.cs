namespace VCore.Factories
{
  public interface IViewModelsFactory
  {
    TModel Create<TModel>(params object[] parameters);
  }
}