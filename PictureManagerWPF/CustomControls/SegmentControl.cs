using PictureManager.Commands;
using PictureManager.Domain.Models;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MH.UI.WPF.Controls;

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

      PreviewMouseDoubleClick += (_, e) => {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        MediaItemsCommands.ViewMediaItemsWithSegmentCommand.Execute(DataContext, this);
      };

      if (Template.FindName("PART_Border", this) is Border b)
        b.ToolTipOpening += (_, _) => ReloadMediaItemSegmentRects();

      if (Template.FindName("PART_ImageGrid", this) is Grid imgGrid)
        imgGrid.PreviewMouseUp += (o, e) => Selected?.Invoke(o, e);

      if (Template.FindName("PART_BtnDetail", this) is IconButton btnDetail)
        btnDetail.Click += (_, _) => {
          if (DataContext is Segment segment && segment.Person != null)
            App.Ui.PeopleBaseVM.Current = App.Ui.PeopleBaseVM.All[segment.Person.Id];
        };
          

      if (Template.FindName("PART_BtnSamePerson", this) is IconButton btnSamePerson)
        btnSamePerson.Click += (_, _) => {
          App.Core.Segments.SetSelectedAsSamePerson();
          App.Core.Segments.DeselectAll();
          AppCore.OnSetPerson?.Invoke(null, EventArgs.Empty);
        };
    }

    public void ReloadMediaItemSegmentRects() {
      if (DataContext is not Segment segment || segment.MediaItem?.Segments == null) return;

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
