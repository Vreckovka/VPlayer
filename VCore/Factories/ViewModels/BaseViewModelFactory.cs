using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ninject;
using Ninject.Parameters;

namespace VCore.Factories
{
  public class BaseViewModelsFactory : IViewModelsFactory
  {
    private readonly IKernel kernel;

    public BaseViewModelsFactory(IKernel kernel)
    {
      this.kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
    }

    public TViewModel Create<TViewModel>(params object[] argumentValue)
    {
      //var constructorArgument = GetConstructorArgument<TViewModel>(argumentValue[0]);

      var constructorArguments = GetConstructors<TViewModel>(argumentValue);

      return kernel.Get<TViewModel>(constructorArguments.ToArray());
    }

    class MyEquilityTester : IEqualityComparer<ParameterInfo>
    {
      public bool Equals(ParameterInfo x, ParameterInfo y)
      {
        return x.ParameterType == y.ParameterType;
      }

      public int GetHashCode(ParameterInfo obj)
      {
        return obj.GetHashCode();
      }
    }

    private IEnumerable<ConstructorArgument> GetConstructors<TConstructedType>(params object[] parametersTypes)
    {
      List<ConstructorArgument> constructorArguments = new List<ConstructorArgument>();

      foreach (var parameter in parametersTypes)
      {
        var argument = GetConstructorArgument<TConstructedType>(parameter);

        if (argument != null)
          constructorArguments.Add(argument);
      }

      return constructorArguments;
    }

    protected ConstructorArgument GetConstructorArgument<TConstructedType>(object argumentValue)
    {
      Type argumentType = null;

      if (argumentValue != null)
      {
        if (argumentValue.GetType().Assembly.IsDynamic)
        {
          argumentType = argumentValue.GetType().BaseType;
        }
        else
          argumentType = argumentValue.GetType();


        var argumentName = GetConstructorArgumentName<TConstructedType>(argumentType);

        var constructorArgument = new ConstructorArgument(argumentName, argumentValue);

        return constructorArgument;
      }

      return null;
    }

    protected string GetConstructorArgumentName<TConstructedType>(Type argumentType)
    {
      var constructedType = typeof(TConstructedType);

      return GetConstructorArgumentName(constructedType, argumentType);
    }

    protected string GetConstructorArgumentName(Type constructedType, Type argumentType)
    {
      var cacheKey = Tuple.Create(constructedType, argumentType);

      if (!ConstructorArgumentNamesCache.ContainsKey(cacheKey))
      {
        var constructorArguments = constructedType.GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Where(p => p.ParameterType == argumentType)
            .DistinctBy(p => p.Name)
            .ToList();

        if (constructorArguments.Count == 0)
          throw new ArgumentException("Could not find constructor with specified argument", "TArgumentType");

        if (constructorArguments.Count > 1)
          throw new ArgumentException("Found multiple arguments of given type with different names", "TArgumentType");

        ConstructorArgumentNamesCache.TryAdd(cacheKey, constructorArguments[0].Name);
      }

      return ConstructorArgumentNamesCache[cacheKey];
    }

    #region ConstructorArgumentNamesCache

    private ConcurrentDictionary<Tuple<Type, Type>, string> constructorArgumentNamesCache;

    private ConcurrentDictionary<Tuple<Type, Type>, string> ConstructorArgumentNamesCache
    {
      get
      {
        if (constructorArgumentNamesCache == null)
          constructorArgumentNamesCache = new ConcurrentDictionary<Tuple<Type, Type>, string>();

        return constructorArgumentNamesCache;
      }
    }

    #endregion


  }
}
