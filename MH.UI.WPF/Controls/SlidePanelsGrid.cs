using System;
using System.Windows;
using System.Windows.Controls;

namespace MH.UI.WPF.Controls {
  public class SlidePanelsGrid : Control {
    public static readonly DependencyProperty ContentLeftProperty = DependencyProperty.Register(
      nameof(ContentLeft),
      typeof(SlidePanel),
      typeof(SlidePanelsGrid));

    public static readonly DependencyProperty ContentMiddleProperty = DependencyProperty.Register(
      nameof(ContentMiddle),
      typeof(FrameworkElement),
      typeof(SlidePanelsGrid));

    public static readonly DependencyProperty ContentRightProperty = DependencyProperty.Register(
      nameof(ContentRight),
      typeof(SlidePanel),
      typeof(SlidePanelsGrid));

    public static readonly DependencyProperty GridSplitterWidthProperty = DependencyProperty.Register(
      nameof(GridSplitterWidth),
      typeof(int),
      typeof(SlidePanelsGrid),
      new PropertyMetadata(3));

    public SlidePanel ContentLeft {
      get => (SlidePanel)GetValue(ContentLeftProperty);
      set => SetValue(ContentLeftProperty, value);
    }

    public FrameworkElement ContentMiddle {
      get => (FrameworkElement)GetValue(ContentMiddleProperty);
      set => SetValue(ContentMiddleProperty, value);
    }

    public SlidePanel ContentRight {
      get => (SlidePanel)GetValue(ContentRightProperty);
      set => SetValue(ContentRightProperty, value);
    }

    public int GridSplitterWidth {
      get => (int)GetValue(GridSplitterWidthProperty);
      set => SetValue(GridSplitterWidthProperty, value);
    }

    public event EventHandler ContentLeftWidthChangedEventHandler = delegate { };
    public event EventHandler ContentRightWidthChangedEventHandler = delegate { };

    private Grid _mainGrid;

    static SlidePanelsGrid() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(SlidePanelsGrid), new FrameworkPropertyMetadata(typeof(SlidePanelsGrid)));
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();

      // open SlidePanel if is not open and mouse cursor is close to left or right edge of SlidePanelsGrid
      MouseMove += (o, e) => {
        var pos = e.GetPosition(this);

        // to stop opening/closing panel by it self in some cases
        if ((pos.X == 0 && pos.Y == 0) || pos.X < 0 || pos.Y < 0) return;

        if (ContentLeft != null) {
          if (!ContentLeft.IsOpen) {
            if (pos.X < 5)
              ContentLeft.IsOpen = true;
          }
          else if (!ContentLeft.IsPinned && pos.X > ContentLeft.ActualWidth)
            ContentLeft.IsOpen = false;
        }

        if (ContentRight != null) {
          if (!ContentRight.IsOpen) {
            if (pos.X > ActualWidth - 5)
              ContentRight.IsOpen = true;
          }
          else if (!ContentRight.IsPinned && pos.X < (ActualWidth - ContentRight.ActualWidth))
            ContentRight.IsOpen = false;
        }
      };

      if (Template.FindName("PART_MainGrid", this) is Grid mainGrid) {
        _mainGrid = mainGrid;
      }

      if (Template.FindName("PART_GridSplitterLeft", this) is GridSplitter gsLeft) {
        gsLeft.DragCompleted += delegate { UpdateContentLeftWidth(); };
      }

      if (Template.FindName("PART_GridSplitterRight", this) is GridSplitter gsRight) {
        gsRight.DragCompleted += delegate { UpdateContentRightWidth(); };
      }

      if (ContentLeft != null) {
        _mainGrid.ColumnDefinitions[0].Width = new(ContentLeft.Width);
        _mainGrid.ColumnDefinitions[1].Width = new(GridSplitterWidth);

        ContentLeft.IsPinnedChangedEventHandler += delegate {
          _mainGrid.ColumnDefinitions[0].Width = new(ContentLeft.IsPinned ? ContentLeft.Width : 0);
          _mainGrid.ColumnDefinitions[1].Width = new(ContentLeft.IsPinned ? GridSplitterWidth : 0);
        };
      }

      if (ContentRight != null) {
        _mainGrid.ColumnDefinitions[3].Width = new(GridSplitterWidth);
        _mainGrid.ColumnDefinitions[4].Width = new(ContentRight.Width);

        ContentRight.IsPinnedChangedEventHandler += delegate {
          _mainGrid.ColumnDefinitions[3].Width = new(ContentRight.IsPinned ? GridSplitterWidth : 0);
          _mainGrid.ColumnDefinitions[4].Width = new(ContentRight.IsPinned ? ContentRight.Width : 0);
        };
      }
    }

    private void UpdateContentLeftWidth() {
      if (ContentLeft == null) return;
      _mainGrid.UpdateLayout();
      ContentLeft.Width = _mainGrid.ColumnDefinitions[0].ActualWidth;
      ContentLeft.UpdateAnimation();
      ContentLeftWidthChangedEventHandler(this, EventArgs.Empty);
    }

    private void UpdateContentRightWidth() {
      if (ContentRight == null) return;
      _mainGrid.UpdateLayout();
      ContentRight.Width = _mainGrid.ColumnDefinitions[4].ActualWidth;
      ContentRight.UpdateAnimation();
      ContentRightWidthChangedEventHandler(this, EventArgs.Empty);
    }
  }
}
