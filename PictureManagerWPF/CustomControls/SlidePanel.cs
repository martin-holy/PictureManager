using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace PictureManager.CustomControls {
  public class SlidePanel : Control, INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public static readonly DependencyProperty BorderMarginProperty = DependencyProperty.Register(nameof(BorderMargin), typeof(Thickness), typeof(SlidePanel));
    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(nameof(Content), typeof(FrameworkElement), typeof(SlidePanel));
    public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(nameof(Position), typeof(Dock), typeof(SlidePanel));

    private bool _isOpen;
    private bool _isPinned;
    private ThicknessAnimation _openAnimation;
    private ThicknessAnimation _closeAnimation;
    private Storyboard _sbOpen;
    private Storyboard _sbClose;

    public Thickness BorderMargin {
      get => (Thickness)GetValue(BorderMarginProperty);
      set => SetValue(BorderMarginProperty, value);
    }

    public FrameworkElement Content {
      get => (FrameworkElement)GetValue(ContentProperty);
      set => SetValue(ContentProperty, value);
    }

    public bool IsOpen {
      get => _isOpen;
      set {
        if (value && CanOpen?.Invoke() == false)
          return;

        _isOpen = value;
        OnPropertyChanged();
        if (value)
          _sbOpen?.Begin(this);
        else
          _sbClose?.Begin(this);
      }
    }

    public bool IsPinned {
      get => _isPinned;
      set {
        _isPinned = value;
        OnPropertyChanged();
        OnIsPinnedChanged?.Invoke(this, EventArgs.Empty);
      }
    }

    public Dock Position {
      get => (Dock)GetValue(PositionProperty);
      set => SetValue(PositionProperty, value);
    }

    public EventHandler OnIsPinnedChanged { get; set; }
    public Func<bool> CanOpen { get; set; }

    static SlidePanel() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(SlidePanel), new FrameworkPropertyMetadata(typeof(SlidePanel)));
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();

      MouseLeave += delegate {
        if (!IsPinned && IsOpen) IsOpen = false;
      };

      UpdateAnimation();

      Storyboard.SetTargetProperty(_openAnimation, new(MarginProperty));
      _sbOpen = new();
      _sbOpen.Children.Add(_openAnimation);

      Storyboard.SetTargetProperty(_closeAnimation, new(MarginProperty));
      _sbClose = new();
      _sbClose.Children.Add(_closeAnimation);

      OnIsPinnedChanged?.Invoke(this, EventArgs.Empty);
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

      var th = Position switch {
        Dock.Left => new Thickness(Width * -1, Margin.Top, 0, Margin.Bottom),
        Dock.Right => new Thickness(0, Margin.Top, Width * -1, Margin.Bottom),
        _ => new Thickness()
      };

      _openAnimation.From = th;
      _closeAnimation.To = th;
    }
  }
}
