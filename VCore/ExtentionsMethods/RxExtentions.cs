using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCore.ExtentionsMethods
{
  public static class RxExtentions
  {
    public static IObservable<TResult> ObservePropertyChange<T, TResult>
      (this T target, Expression<Func<T, TResult>> property) 
      where T : INotifyPropertyChanged
    {
      var me = property.Body as MemberExpression;

      if (me == null)
        throw new NotSupportedException("Only use expressions that call a single property");

      var propertyName = me.Member.Name;

      var getValueFunc = property.Compile();

      return Observable.Create<TResult>(o => {
        PropertyChangedEventHandler eventHandler = new PropertyChangedEventHandler((s, pce) => {
          if (pce.PropertyName == null || pce.PropertyName == propertyName)
            o.OnNext(getValueFunc(target));
        });
        target.PropertyChanged += eventHandler;
        return () => target.PropertyChanged -= eventHandler;
      });
    }
  }
}
