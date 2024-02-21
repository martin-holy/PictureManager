using MH.Utils.BaseClasses;
using System;

namespace MH.UI.Controls;

public class SlidePanel : ObservableObject {
  private bool _canOpen = true;
  private bool _isOpen;
  private bool _isPinned;
  private double _size;

  public object Content { get; }
  public Position Position { get; }
  public bool CanOpen { get => _canOpen; set { _canOpen = value; OnCanOpenChanged(); } }
  public bool IsOpen { get => _isOpen; set => OnIsOpenChanged(value); }
  public bool IsPinned { get => _isPinned; set { _isPinned = value; OnIsPinnedChanged(); } }
  public double Size { get => _size; private set { _size = value; OnPropertyChanged(); } }

  public SlidePanel(Position position, object content, double size) {
    Position = position;
    Content = content;
    Size = size;
  }

  private void OnCanOpenChanged() =>
    IsOpen = CanOpen && IsPinned;

  private void OnIsOpenChanged(bool value) {
    if (value.Equals(_isOpen)) return;
    _isOpen = value;
    if (!IsOpen && IsPinned) IsPinned = false;
    OnPropertyChanged(nameof(IsOpen));
  }

  private void OnIsPinnedChanged() {
    OnPropertyChanged(nameof(IsPinned));
    IsOpen = IsPinned;
  }

  public void SetSize(double size) {
    if (size != 0 && !size.Equals(Size)) Size = size;
  }

  public void OnMouseMove(Func<double, bool> mouseOut, bool mouseOnEdge) {
    if (IsPinned) return;
    if (mouseOut(Size)) IsOpen = false;
    else if (mouseOnEdge && CanOpen) IsOpen = true;
  }
}