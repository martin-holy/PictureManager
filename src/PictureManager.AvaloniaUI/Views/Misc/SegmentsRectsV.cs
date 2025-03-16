using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using MH.UI.AvaloniaUI.Utils;
using PictureManager.Common.Features.Segment;

namespace PictureManager.AvaloniaUI.Views.Misc;

public sealed class SegmentsRectsV : ItemsControl {
  private SegmentRectS? _segmentRectS;

  protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) {
    base.OnAttachedToVisualTree(e);
    _segmentRectS = DataContext as SegmentRectS;

    if (this.FindAncestorOfType<Grid>() is not { } grid) return;
    grid.AddHandler(PointerPressedEvent, _onPointerPressed, RoutingStrategies.Tunnel);
    grid.AddHandler(PointerMovedEvent, _onPointerMoved, RoutingStrategies.Tunnel);
    grid.AddHandler(PointerReleasedEvent, _onPointerReleased, RoutingStrategies.Tunnel);
  }

  protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) {
    base.OnDetachedFromVisualTree(e);

    if (this.FindAncestorOfType<Grid>() is not { } grid) return;
    grid.RemoveHandler(PointerPressedEvent, _onPointerPressed);
    grid.RemoveHandler(PointerMovedEvent, _onPointerMoved);
    grid.RemoveHandler(PointerReleasedEvent, _onPointerReleased);
  }

  private void _onPointerPressed(object? sender, PointerPressedEventArgs e) {
    if (_segmentRectS is not { AreVisible: true }) return;
    var point = e.GetCurrentPoint(this);

    if (e.Source is Control { DataContext: SegmentRectM sr } c
        && ("PART_ResizeBorder".Equals(c.Name) || c.FindAncestorOfType<PathIcon>() is { Name: "PART_MovePoint" })) {
      _segmentRectS.SetCurrent(sr, point.Position.X, point.Position.Y);
      return;
    }

    if ((point.Properties.IsLeftButtonPressed && KeyboardHelper.IsCtrlOn) || point.Properties.IsRightButtonPressed)
      _segmentRectS.CreateNew(point.Position.X, point.Position.Y);
  }

  private void _onPointerMoved(object? sender, PointerEventArgs e) {
    if (_segmentRectS?.Current == null) return;
    var point = e.GetCurrentPoint(this);

    if (point.Properties is { IsRightButtonPressed: false, IsLeftButtonPressed: false }) {
      _segmentRectS.EndEdit();
      return;
    }

    e.Handled = true;
    _segmentRectS.Edit(point.Position.X, point.Position.Y);
  }

  private void _onPointerReleased(object? sender, PointerReleasedEventArgs e) {
    _segmentRectS?.EndEdit();
  }
}