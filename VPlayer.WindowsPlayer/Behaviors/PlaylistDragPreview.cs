using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace VPlayer.WindowsPlayer.Behaviors
{
  // Only transforms realized containers. The queue and database are untouched until drop.
  internal sealed class PlaylistDragPreview : IDisposable
  {
    private readonly ListView list;
    private readonly object sourceItem;
    private readonly double rowHeight;
    private readonly Point grabOffset;
    private readonly Dictionary<ListViewItem, RowVisual> visuals = new Dictionary<ListViewItem, RowVisual>();
    private readonly AdornerLayer layer;
    private readonly DragAdorner adorner;
    private readonly ScrollViewer scroll;
    private readonly DispatcherTimer scrollTimer;
    private Point pointer;
    private bool disposed;

    public int SourceIndex { get; }
    public int TargetIndex { get; private set; }
    public bool IsVisible { get; private set; }

    public PlaylistDragPreview(ListView list, ListViewItem source, int sourceIndex, Point start)
    {
      this.list = list;
      sourceItem = source.DataContext;
      SourceIndex = TargetIndex = sourceIndex;
      rowHeight = source.ActualHeight + source.Margin.Top + source.Margin.Bottom;
      var origin = source.TranslatePoint(new Point(), list);
      grabOffset = new Point(start.X - origin.X, start.Y - origin.Y);
      var dpi = VisualTreeHelper.GetDpi(source);
      var bitmap = new RenderTargetBitmap(Math.Max(1, (int)Math.Ceiling(source.ActualWidth * dpi.DpiScaleX)),
        Math.Max(1, (int)Math.Ceiling(source.ActualHeight * dpi.DpiScaleY)), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
      var snapshotVisual = new DrawingVisual();
      using (var drawing = snapshotVisual.RenderOpen())
        drawing.DrawRectangle(new VisualBrush(source) { Stretch = Stretch.Fill }, null,
          new Rect(0, 0, source.ActualWidth, source.ActualHeight));
      bitmap.Render(snapshotVisual);
      bitmap.Freeze();
      adorner = new DragAdorner(list, bitmap, new Size(source.ActualWidth, source.ActualHeight));
      layer = AdornerLayer.GetAdornerLayer(list);
      layer?.Add(adorner);
      scroll = Descendants<ScrollViewer>(list).FirstOrDefault();
      scrollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(60) };
      scrollTimer.Tick += ScrollAtEdge;
      scrollTimer.Start();
    }

    public void Update(Point position)
    {
      if (disposed) return;
      pointer = position;
      if (!new Rect(list.RenderSize).Contains(position)) { Hide(); return; }
      IsVisible = true;
      var rows = Descendants<ListViewItem>(list)
        .Where(x => ItemsControl.ItemsControlFromItemContainer(x) == list && x.ActualHeight > 0).ToList();
      // Recycled or unloaded containers must never retain another track's drag transform.
      foreach (var pair in visuals.ToList())
        if (!rows.Contains(pair.Key) || !ReferenceEquals(pair.Key.DataContext, pair.Value.Item))
        { pair.Value.Restore(); visuals.Remove(pair.Key); }
      foreach (var row in rows)
        if (!visuals.ContainsKey(row)) visuals.Add(row, new RowVisual(row));

      var slots = rows.Select(row => new Slot
      {
        Row = row,
        Index = list.ItemContainerGenerator.IndexFromContainer(row),
        Top = row.TranslatePoint(new Point(), list).Y - visuals[row].Shift.Y - row.Margin.Top,
        Height = row.ActualHeight + row.Margin.Top + row.Margin.Bottom
      }).Where(x => x.Index >= 0).OrderBy(x => x.Index).ToList();
      if (slots.Count == 0) { Hide(); return; }
      int insertion = slots.Last().Index + 1;
      foreach (var slot in slots)
      {
        if (slot.Index == SourceIndex) continue;
        if (position.Y < slot.Top + slot.Height / 2) { insertion = slot.Index; break; }
      }
      TargetIndex = Math.Max(0, Math.Min(list.Items.Count - 1, insertion > SourceIndex ? insertion - 1 : insertion));
      var target = slots.FirstOrDefault(x => x.Index == TargetIndex);
      double gapTop = target != null ? target.Top : slots.Last().Top + slots.Last().Height - rowHeight;
      if (TargetIndex > SourceIndex && target != null) gapTop += target.Height - rowHeight;
      foreach (var slot in slots)
      {
        var state = visuals[slot.Row];
        state.SetHidden(ReferenceEquals(slot.Row.DataContext, sourceItem));
        double shift = 0;
        if (TargetIndex > SourceIndex && slot.Index > SourceIndex && slot.Index <= TargetIndex) shift = -rowHeight;
        if (TargetIndex < SourceIndex && slot.Index >= TargetIndex && slot.Index < SourceIndex) shift = rowHeight;
        state.MoveTo(shift);
      }
      adorner.Show(position - (Vector)grabOffset, gapTop, rowHeight);
    }

    public void Hide()
    {
      IsVisible = false;
      foreach (var state in visuals.Values) { state.SetHidden(false); state.MoveTo(0); }
      adorner.Visibility = Visibility.Hidden;
    }

    private void ScrollAtEdge(object sender, EventArgs e)
    {
      if (!IsVisible || disposed || scroll == null) return;
      double step = pointer.Y < 32 ? -12 : pointer.Y > list.ActualHeight - 32 ? 12 : 0;
      if (step == 0) return;
      if (scroll.CanContentScroll && VirtualizingPanel.GetScrollUnit(list) == ScrollUnit.Item)
        step = Math.Sign(step);
      scroll.ScrollToVerticalOffset(scroll.VerticalOffset + step);
      list.UpdateLayout();
      Update(pointer);
    }

    public void Dispose()
    {
      if (disposed) return;
      disposed = true;
      IsVisible = false;
      scrollTimer.Stop();
      scrollTimer.Tick -= ScrollAtEdge;
      foreach (var state in visuals.Values) state.Restore();
      visuals.Clear();
      layer?.Remove(adorner);
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
      for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
      {
        var child = VisualTreeHelper.GetChild(parent, i);
        if (child is T found) yield return found;
        foreach (var descendant in Descendants<T>(child)) yield return descendant;
      }
    }

    private sealed class Slot
    {
      public ListViewItem Row;
      public int Index;
      public double Top;
      public double Height;
    }

    private sealed class RowVisual
    {
      private readonly ListViewItem row;
      private readonly Transform originalTransform;
      private readonly object localTransform;
      private readonly object localOpacity;
      private readonly double opacity;
      private double destination;
      public object Item { get; }
      public TranslateTransform Shift { get; } = new TranslateTransform();

      public RowVisual(ListViewItem row)
      {
        this.row = row;
        Item = row.DataContext;
        originalTransform = row.RenderTransform;
        localTransform = row.ReadLocalValue(UIElement.RenderTransformProperty);
        localOpacity = row.ReadLocalValue(UIElement.OpacityProperty);
        opacity = row.Opacity;
        var group = new TransformGroup();
        if (originalTransform != null) group.Children.Add(originalTransform);
        group.Children.Add(Shift);
        row.SetCurrentValue(UIElement.RenderTransformProperty, group);
      }

      public void SetHidden(bool hidden) => row.SetCurrentValue(UIElement.OpacityProperty, hidden ? 0d : opacity);
      public void MoveTo(double value)
      {
        if (Math.Abs(destination - value) < 0.1) return;
        destination = value;
        Shift.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(value, TimeSpan.FromMilliseconds(160))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } }, HandoffBehavior.SnapshotAndReplace);
      }
      public void Restore()
      {
        Shift.BeginAnimation(TranslateTransform.YProperty, null);
        if (localTransform == DependencyProperty.UnsetValue) row.ClearValue(UIElement.RenderTransformProperty);
        else row.SetCurrentValue(UIElement.RenderTransformProperty, originalTransform);
        if (localOpacity == DependencyProperty.UnsetValue) row.ClearValue(UIElement.OpacityProperty);
        else row.SetCurrentValue(UIElement.OpacityProperty, opacity);
      }
    }

    private sealed class DragAdorner : Adorner
    {
      private readonly ImageSource snapshot;
      private readonly Size imageSize;
      private Point position;
      private double gapHeight;
      private double targetTop = double.NaN;
      public static readonly DependencyProperty GapTopProperty = DependencyProperty.Register(nameof(GapTop), typeof(double), typeof(DragAdorner), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));
      public double GapTop { get => (double)GetValue(GapTopProperty); set => SetValue(GapTopProperty, value); }

      public DragAdorner(UIElement element, ImageSource snapshot, Size size) : base(element)
      { this.snapshot = snapshot; imageSize = size; IsHitTestVisible = false; }

      public void Show(Point point, double top, double height)
      {
        position = point;
        gapHeight = height;
        Visibility = Visibility.Visible;
        if (double.IsNaN(targetTop)) GapTop = top;
        else if (Math.Abs(targetTop - top) > 0.1)
          BeginAnimation(GapTopProperty, new DoubleAnimation(top, TimeSpan.FromMilliseconds(160))
          { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } }, HandoffBehavior.SnapshotAndReplace);
        targetTop = top;
        InvalidateVisual();
      }

      protected override void OnRender(DrawingContext dc)
      {
        double width = AdornedElement.RenderSize.Width;
        dc.PushClip(new RectangleGeometry(new Rect(AdornedElement.RenderSize)));
        var accent = new SolidColorBrush(Color.FromRgb(220, 169, 94));
        var gap = new Rect(4, GapTop + 2, Math.Max(0, width - 8), Math.Max(0, gapHeight - 4));
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(18, 220, 169, 94)),
          new Pen(accent, 1.5) { DashStyle = DashStyles.Dash }, gap, 5, 5);
        var card = new Rect(Math.Max(4, Math.Min(position.X, width - imageSize.Width - 4)), position.Y, Math.Min(imageSize.Width, Math.Max(0, width - 8)), imageSize.Height);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)), null,
          new Rect(card.X + 3, card.Y + 5, card.Width, card.Height), 5, 5);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(42, 40, 38)), new Pen(accent, 1), card, 5, 5);
        dc.PushOpacity(0.92);
        dc.DrawImage(snapshot, card);
        dc.Pop();
        dc.Pop();
      }
    }
  }
}
