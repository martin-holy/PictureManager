using MH.Utils.BaseClasses;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace MH.UI.WPF.Controls {
  public class SlidePanel : ContentControl {
    public static readonly DependencyProperty CanOpenProperty =
      DependencyProperty.Register(nameof(CanOpen), typeof(bool), typeof(SlidePanel), new(true, CanOpenChanged));

    public static readonly DependencyProperty IsPinnedProperty =
      DependencyProperty.Register(nameof(IsPinned), typeof(bool), typeof(SlidePanel), new(IsPinnedChanged));

    public static readonly DependencyProperty IsOpenProperty =
      DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(SlidePanel), new(IsOpenChanged));

    public static readonly DependencyProperty PositionProperty =
      DependencyProperty.Register(nameof(Position), typeof(Dock), typeof(SlidePanel));

    public bool CanOpen {
      get => (bool)GetValue(CanOpenProperty);
      set => SetValue(CanOpenProperty, value);
    }

    public bool IsPinned {
      get => (bool)GetValue(IsPinnedProperty);
      set => SetValue(IsPinnedProperty, value);
    }

    public bool IsOpen {
      get => (bool)GetValue(IsOpenProperty);
      set => SetValue(IsOpenProperty, value);
    }

    public Dock Position {
      get => (Dock)GetValue(PositionProperty);
      set => SetValue(PositionProperty, value);
    }

    public event EventHandler IsPinnedChangedEventHandler = delegate { };
    public Storyboard SbOpen { get; set; }
    public Storyboard SbClose { get; set; }
    public RelayCommand<object> PinCommand { get; set; }

    private ThicknessAnimation _openAnimation;
    private ThicknessAnimation _closeAnimation;

    static SlidePanel() {
      DefaultStyleKeyProperty.OverrideMetadata(
        typeof(SlidePanel),
        new FrameworkPropertyMetadata(typeof(SlidePanel)));
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();
      PinCommand = new(() => IsPinned = !IsPinned);
      MouseLeave += delegate { if (!IsPinned) IsOpen = false; };
      SizeChanged += (o, e) => { UpdateAnimation(e); };

      _openAnimation = new();
      _closeAnimation = new();

      Storyboard.SetTargetProperty(_openAnimation, new(MarginProperty));
      SbOpen = new();
      SbOpen.Children.Add(_openAnimation);

      Storyboard.SetTargetProperty(_closeAnimation, new(MarginProperty));
      SbClose = new();
      SbClose.Children.Add(_closeAnimation);
    }

    private static void CanOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
      if (d is not SlidePanel panel) return;

      if (panel.CanOpen) {
        if (panel.IsPinned && !panel.IsOpen) {
          panel.IsOpen = true;
          panel.IsPinnedChangedEventHandler(d, EventArgs.Empty);
        }
      }
      else if (panel.IsOpen) {
        panel.IsOpen = false;
        panel.IsPinnedChangedEventHandler(d, EventArgs.Empty);
      }
    }

    private static void IsPinnedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
      if (d is not SlidePanel panel) return;

      if (panel.IsOpen != panel.IsPinned)
        panel.IsOpen = panel.IsPinned;

      if (!panel.IsInitialized) return;

      panel.IsPinnedChangedEventHandler(d, EventArgs.Empty);
    }

    private static void IsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
      if (d is not SlidePanel panel) return;

      if (panel.IsOpen)
        panel.SbOpen?.Begin(panel);
      else
        panel.SbClose?.Begin(panel);
    }

    public void SetWidth(double width) {
      if (width == 0 || width == Width) return;

      Width = width;
    }

    public void UpdateAnimation(SizeChangedEventArgs e) {
      if (((Position == Dock.Top || Position == Dock.Bottom) && !e.HeightChanged) ||
        ((Position == Dock.Left || Position == Dock.Right) && !e.WidthChanged))
        return;

      double size = 0;
      Thickness openFrom = new(0);
      Thickness openTo = new(0);
      Thickness closeFrom = new(0);
      Thickness closeTo = new(0);

      if (Position == Dock.Top || Position == Dock.Bottom) {
        if (ActualHeight != 0) size = ActualHeight;
        else if (!double.IsNaN(Height)) size = Height;
        else if (MinHeight != 0) size = MinHeight;

        if (Position == Dock.Top) {
          openFrom.Top = size * -1;
          closeTo.Top = size * -1;
        }
        else {
          openFrom.Bottom = size * -1;
          closeTo.Bottom = size * -1;
        }
      }
      else {
        if (ActualWidth != 0) size = ActualWidth;
        else if (!double.IsNaN(Width)) size = Width;
        else if (MinWidth != 0) size = MinWidth;

        if (Position == Dock.Left) {
          openFrom.Left = size * -1;
          closeTo.Left = size * -1;
        }
        else {
          openFrom.Right = size * -1;
          closeTo.Right = size * -1;
        }
      }

      var duration = new Duration(TimeSpan.FromMilliseconds(size * 0.7));

      _openAnimation.Duration = duration;
      _openAnimation.From = openFrom;
      _openAnimation.To = openTo;
      _closeAnimation.Duration = duration;
      _closeAnimation.From = closeFrom;
      _closeAnimation.To = closeTo;

      if (!IsOpen)
        SbClose?.Begin(this);
    }
  }
}
