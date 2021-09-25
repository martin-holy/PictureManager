using PictureManager.Commands;
using PictureManager.Domain.Models;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PictureManager.CustomControls {
  public class SegmentControl : Control {
    public static readonly DependencyProperty IsCheckmarkVisibleProperty = DependencyProperty.Register(nameof(IsCheckmarkVisible), typeof(bool), typeof(SegmentControl), new PropertyMetadata(false));

    public bool IsCheckmarkVisible {
      get => (bool)GetValue(IsCheckmarkVisibleProperty);
      set => SetValue(IsCheckmarkVisibleProperty, value);
    }

    public ObservableCollection<Tuple<Int32Rect, bool>> MediaItemSegmentRects { get; } = new();
    public EventHandler<MouseButtonEventArgs> Selected { get; set; }

    static SegmentControl() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(SegmentControl), new FrameworkPropertyMetadata(typeof(SegmentControl)));
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();

      PreviewMouseDoubleClick += (o, e) => {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        MediaItemsCommands.ViewMediaItemsWithSegmentCommand.Execute(DataContext, this);
      };

      if (Template.FindName("PART_Border", this) is Border b)
        b.ToolTipOpening += (o, e) => ReloadMediaItemSegmentRects();

      if (Template.FindName("PART_ImageGrid", this) is Grid imgGrid)
        imgGrid.PreviewMouseUp += (o, e) => Selected?.Invoke(o, e);

      if (Template.FindName("PART_BtnDetail", this) is Button btnDetail)
        btnDetail.Click += (o, e) => _ = App.Core.People.Current = (DataContext as Segment)?.Person;

      if (Template.FindName("PART_BtnSamePerson", this) is Button btnSamePerson)
        btnSamePerson.Click += (o, e) => {
          App.Core.Segments.SetSelectedAsSamePerson();
          App.Core.Segments.DeselectAll();
          AppCore.OnSetPerson?.Invoke(null, EventArgs.Empty);
        };
    }

    public void ReloadMediaItemSegmentRects() {
      var segment = DataContext as Segment;
      if (segment == null || segment.MediaItem.Segments == null) return;

      var scale = segment.MediaItem.Width / (double)segment.MediaItem.ThumbWidth;
      MediaItemSegmentRects.Clear();

      foreach (var s in segment.MediaItem.Segments) {
        var sRect = s.ToRect();
        var rect = new Int32Rect((int)(sRect.X / scale), (int)(sRect.Y / scale), (int)(sRect.Width / scale), (int)(sRect.Height / scale));
        MediaItemSegmentRects.Add(new Tuple<Int32Rect, bool>(rect, s == segment));
      }
    }
  }
}
