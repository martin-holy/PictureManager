using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace MH.UI.WPF.Controls {
  public class SlidePanel : Control {
    private ThicknessAnimation _openAnimation;
    private ThicknessAnimation _closeAnimation;
    public Storyboard SbOpen { get; set; }
    public Storyboard SbClose { get; set; }

    public static readonly DependencyProperty BorderMarginProperty = DependencyProperty.Register(
      nameof(BorderMargin),
      typeof(Thickness),
      typeof(SlidePanel));

    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
      nameof(Content),
      typeof(FrameworkElement),
      typeof(SlidePanel));

    public static readonly DependencyProperty IsPinnedProperty = DependencyProperty.Register(
      nameof(IsPinned),
      typeof(bool),
      typeof(SlidePanel),
      new(IsPinnedChanged));

    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
      nameof(IsOpen),
      typeof(bool),
      typeof(SlidePanel),
      new(IsOpenChanged));

    public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
      nameof(Position),
      typeof(Dock),
      typeof(SlidePanel));

    public Thickness BorderMargin {
      get => (Thickness)GetValue(BorderMarginProperty);
      set => SetValue(BorderMarginProperty, value);
    }

    public FrameworkElement Content {
      get => (FrameworkElement)GetValue(ContentProperty);
      set => SetValue(ContentProperty, value);
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
    public Func<bool> CanOpen { get; set; }

    static SlidePanel() {
      DefaultStyleKeyProperty.OverrideMetadata(
        typeof(SlidePanel),
        new FrameworkPropertyMetadata(typeof(SlidePanel)));
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();
      UpdateAnimation();

      Storyboard.SetTargetProperty(_openAnimation, new(MarginProperty));
      SbOpen = new();
      SbOpen.Children.Add(_openAnimation);

      Storyboard.SetTargetProperty(_closeAnimation, new(MarginProperty));
      SbClose = new();
      SbClose.Children.Add(_closeAnimation);

      IsPinnedChangedEventHandler(this, EventArgs.Empty);
    }

    private static void IsPinnedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
      (d as SlidePanel)?.IsPinnedChangedEventHandler(d, EventArgs.Empty);

    private static void IsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
      if (d is not SlidePanel panel) return;

      var value = (e.NewValue as bool?) == true;

      if (value && panel.CanOpen?.Invoke() == false)
        return;

      if (value)
        panel.SbOpen?.Begin(panel);
      else
        panel.SbClose?.Begin(panel);
    }

    // creates new or update existing animation if panel width changes
    public void UpdateAnimation() {
      _openAnimation ??= new() {
        Duration = new(TimeSpan.FromMilliseconds(250)),
        To = new(0, Margin.Top, 0, Margin.Bottom)
      };

      _closeAnimation ??= new() {
        Duration = new(TimeSpan.FromMilliseconds(250)),
        From = new(0, Margin.Top, 0, Margin.Bottom)
      };

      Thickness th = Position switch {
        Dock.Left => new(Width * -1, Margin.Top, 0, Margin.Bottom),
        Dock.Right => new(0, Margin.Top, Width * -1, Margin.Bottom),
        _ => new()
      };

      _openAnimation.From = th;
      _closeAnimation.To = th;
    }
  }
}
