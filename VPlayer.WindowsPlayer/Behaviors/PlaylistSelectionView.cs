using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Markup;

namespace VPlayer.WindowsPlayer.Behaviors
{
  // A virtual queue is replaced as a whole. Index its occurrences once so WPF does not
  // scan the entire queue for each item while restoring a large extended selection.
  public sealed class PlaylistSelectionView : ListCollectionView
  {
    private readonly Dictionary<object, int> indices;
    public PlaylistSelectionView(IList items) : base(items)
    {
      indices = new Dictionary<object, int>(items.Count, new ReferenceComparer());
      for (int i = 0; i < items.Count; i++)
        if (items[i] != null && !indices.ContainsKey(items[i])) indices.Add(items[i], i);
    }
    public override int IndexOf(object item) => indices == null ? base.IndexOf(item) :
      item != null && indices.TryGetValue(item, out var index) ? index : -1;
    public override bool Contains(object item) => IndexOf(item) >= 0;

    private sealed class ReferenceComparer : IEqualityComparer<object>
    {
      public new bool Equals(object x, object y) => ReferenceEquals(x, y);
      public int GetHashCode(object item) => RuntimeHelpers.GetHashCode(item);
    }
  }

  public sealed class PlaylistSelectionViewConverter : MarkupExtension, IValueConverter
  {
    public override object ProvideValue(IServiceProvider serviceProvider) => this;
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
      value is IList items ? new PlaylistSelectionView(items) : null;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}
