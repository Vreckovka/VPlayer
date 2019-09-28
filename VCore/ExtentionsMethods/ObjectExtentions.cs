using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCore.ExtentionsMethods
{
  public static class ObjectExtentions
  {
    public static List<T> AsList<T>(this T obj)
    {
      var list = new List<T>();
      list.Add(obj);
      return list;
    }
  }
}
