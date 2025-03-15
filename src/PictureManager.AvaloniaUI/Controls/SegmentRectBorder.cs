using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace PictureManager.AvaloniaUI.Controls;

public sealed class SegmentRectBorder : Border {
  public static readonly StyledProperty<bool> IsMouseOver2Property =
    AvaloniaProperty.Register<SegmentRectBorder, bool>(nameof(IsMouseOver2));

  public bool IsMouseOver2 { get => GetValue(IsMouseOver2Property); set => SetValue(IsMouseOver2Property, value); }

  protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) {
    base.OnAttachedToVisualTree(e);
    AddHandler(PointerMovedEvent, _onPointerMoved);
  }

  protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) {
    base.OnDetachedFromVisualTree(e);
    RemoveHandler(PointerMovedEvent, _onPointerMoved);
  }

  private void _onPointerMoved(object? sender, PointerEventArgs e) {
    var hitPoint = e.GetPosition(this);
    var w = Bounds.Width;
    var h = Bounds.Height;

    if (w < 20 || h < 20) {
      IsMouseOver2 = true;
      return;
    }

    var borderRect = new Rect(0, 0, w, h);
    var moveBtnRect = new Rect((w / 2) - 10, (h / 2) - 10, 20, 20);
    var innerRect = new Rect(
      borderRect.X + 10,
      borderRect.Y + 10,
      borderRect.Width - 20,
      borderRect.Height - 20
    );

    IsMouseOver2 = innerRect.Contains(hitPoint) || moveBtnRect.Contains(hitPoint);
  }
}