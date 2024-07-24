using MH.UI.Controls;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Dock = MH.UI.Controls.Dock;

namespace MH.UI.WPF.Controls;

public class SlidePanelHost : Control {
  private readonly ThicknessAnimation _openAnimation = new();
  private readonly ThicknessAnimation _closeAnimation = new();
  private readonly Storyboard _sbOpen = new();
  private readonly Storyboard _sbClose = new();

  public static readonly DependencyProperty SlidePanelProperty = DependencyProperty.Register(
    nameof(SlidePanel), typeof(SlidePanel), typeof(SlidePanelHost), new(OnSlidePanelPropertyChanged));

  public SlidePanel SlidePanel {
    get => (SlidePanel)GetValue(SlidePanelProperty);
    set => SetValue(SlidePanelProperty, value);
  }

  public override void OnApplyTemplate() {
    base.OnApplyTemplate();
    Storyboard.SetTargetProperty(_openAnimation, new(MarginProperty));
    Storyboard.SetTargetProperty(_closeAnimation, new(MarginProperty));
    _sbOpen.Children.Add(_openAnimation);
    _sbClose.Children.Add(_closeAnimation);
  }

  private static void OnSlidePanelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
    if (d is not SlidePanelHost self) return;
    if (e.OldValue is ObservableObject oldO) oldO.PropertyChanged -= self.OnAnySlidePanelPropertyChanged;
    if (e.NewValue is ObservableObject newO) newO.PropertyChanged += self.OnAnySlidePanelPropertyChanged;
  }

  private void OnAnySlidePanelPropertyChanged(object? sender, PropertyChangedEventArgs e) {
    if (!e.Is(nameof(SlidePanel.IsOpen))) return;
    if (SlidePanel.IsOpen)
      _sbOpen.Begin(this);
    else
      _sbClose.Begin(this);
  }

  public void UpdateAnimation(SizeChangedEventArgs e) {
    if ((SlidePanel.Dock is Dock.Top or Dock.Bottom && !e.HeightChanged) ||
        (SlidePanel.Dock is Dock.Left or Dock.Right && !e.WidthChanged))
      return;

    var size = SlidePanel.Size * -1;
    var duration = new Duration(TimeSpan.FromMilliseconds(size * -1 * 0.7));
    var openFrom = new Thickness(0);
    var openTo = new Thickness(0);
    var closeFrom = new Thickness(0);
    var closeTo = new Thickness(0);

    switch (SlidePanel.Dock) {
      case Dock.Left: openFrom.Left = size; closeTo.Left = size; break;
      case Dock.Top: openFrom.Top = size; closeTo.Top = size; break;
      case Dock.Right: openFrom.Right = size; closeTo.Right = size; break;
      case Dock.Bottom: openFrom.Bottom = size; closeTo.Bottom = size; break;
      default: throw new ArgumentOutOfRangeException();
    }

    _openAnimation.Duration = duration;
    _openAnimation.From = openFrom;
    _openAnimation.To = openTo;
    _closeAnimation.Duration = duration;
    _closeAnimation.From = closeFrom;
    _closeAnimation.To = closeTo;

    if (!SlidePanel.IsOpen) _sbClose.Begin(this);
  }
}