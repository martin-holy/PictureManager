using PictureManager.Commands;
using PictureManager.Domain.Models;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PictureManager.CustomControls {
  public class FaceControl : Control {
    public static readonly DependencyProperty IsCheckmarkVisibleProperty = DependencyProperty.Register(nameof(IsCheckmarkVisible), typeof(bool), typeof(FaceControl), new PropertyMetadata(false));

    public bool IsCheckmarkVisible {
      get => (bool)GetValue(IsCheckmarkVisibleProperty);
      set => SetValue(IsCheckmarkVisibleProperty, value);
    }

    public ObservableCollection<Tuple<Int32Rect, bool>> MediaItemFaceRects { get; } = new();

    static FaceControl() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(FaceControl), new FrameworkPropertyMetadata(typeof(FaceControl)));
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();

      PreviewMouseDoubleClick += (o, e) => {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        MediaItemsCommands.ViewMediaItemsWithFaceCommand.Execute(DataContext, this);
      };

      if (Template.FindName("PART_Border", this) is Border b)
        b.ToolTipOpening += (o, e) => ReloadMediaItemFaceRects();
    }

    public void ReloadMediaItemFaceRects() {
      var face = DataContext as Face;
      if (face == null || face.MediaItem.Faces == null) return;

      var scale = face.MediaItem.Width / (double)face.MediaItem.ThumbWidth;
      MediaItemFaceRects.Clear();

      foreach (var f in face.MediaItem.Faces) {
        var fRect = f.ToRect();
        var rect = new Int32Rect((int)(fRect.X / scale), (int)(fRect.Y / scale), (int)(fRect.Width / scale), (int)(fRect.Height / scale));
        MediaItemFaceRects.Add(new Tuple<Int32Rect, bool>(rect, f == face));
      }
    }
  }
}
