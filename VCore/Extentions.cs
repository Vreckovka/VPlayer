using System;
using System.Collections.Generic;
using System.Linq;

namespace VCore
{
    public static class Extentions
    {
        #region DistictBy

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return DistinctBy(source, keySelector, EqualityComparer<TKey>.Default);
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (keySelector == null) throw new ArgumentNullException("keySelector");

            var distinctValues = new HashSet<TKey>(comparer);

            return source.Where(item => distinctValues.Add(keySelector(item)));
        }

        #endregion
    }
}