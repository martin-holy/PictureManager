using PictureManager.Commands;
using PictureManager.Domain.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace PictureManager.CustomControls {
  public class FaceControl : Control {
    public static readonly DependencyProperty FaceProperty = DependencyProperty.Register(nameof(Face), typeof(Face), typeof(FaceControl));
    public static readonly DependencyProperty IsCheckmarkVisibleProperty = DependencyProperty.Register(nameof(IsCheckmarkVisible), typeof(bool), typeof(FaceControl), new PropertyMetadata(false));

    public Face Face {
      get => (Face)GetValue(FaceProperty);
      set => SetValue(FaceProperty, value);
    }

    public bool IsCheckmarkVisible {
      get => (bool)GetValue(IsCheckmarkVisibleProperty);
      set => SetValue(IsCheckmarkVisibleProperty, value);
    }

    public ObservableCollection<Int32Rect> MediaItemFaceRects { get; } = new();

    static FaceControl() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(FaceControl), new FrameworkPropertyMetadata(typeof(FaceControl)));
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();

      PreviewMouseDoubleClick += (o, e) => MediaItemsCommands.ViewMediaItemsWithFaceCommand.Execute((Face)DataContext, this);

      if (Template.FindName("PART_Border", this) is Border b)
        b.ToolTipOpening += (o, e) => ReloadMediaItemFaceRects();
    }

    public void ReloadMediaItemFaceRects() {
      if (Face == null) return;
      //if (MediaItemFaceRects.Any()) return;

      var scale = Face.MediaItem.Width / (double)Face.MediaItem.ThumbWidth;
      MediaItemFaceRects.Clear();

      foreach (var f in Face.MediaItem.Faces) {
        var fb = f.FaceBox;
        var rect = new Int32Rect((int)(fb.X / scale), (int)(fb.Y / scale), (int)(fb.Width / scale), (int)(fb.Height / scale));
        MediaItemFaceRects.Add(rect);
      }
    }
  }
}
