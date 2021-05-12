using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PictureManager.CustomControls {
  public class SlidePanelsGrid: Control {

    public static readonly DependencyProperty ContentLeftProperty = DependencyProperty.Register(
      nameof(ContentLeft), typeof(SlidePanel), typeof(SlidePanelsGrid));
    public static readonly DependencyProperty ContentMiddleProperty = DependencyProperty.Register(
      nameof(ContentMiddle), typeof(FrameworkElement), typeof(SlidePanelsGrid));
    public static readonly DependencyProperty ContentRightProperty = DependencyProperty.Register(
      nameof(ContentRight), typeof(SlidePanel), typeof(SlidePanelsGrid));
    public static readonly DependencyProperty GridSplitterWidthProperty = DependencyProperty.Register(
      nameof(GridSplitterWidth), typeof(int), typeof(SlidePanelsGrid), new PropertyMetadata(3));

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
      get => (int) GetValue(GridSplitterWidthProperty);
      set => SetValue(GridSplitterWidthProperty, value);
    }

    public Action OnContentLeftWidthChanged;
    public Action OnContentRightWidthChanged;

    private Grid _mainGrid;

    static SlidePanelsGrid() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(SlidePanelsGrid),
        new FrameworkPropertyMetadata(typeof(SlidePanelsGrid)));
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();

      // open SlidePanel if is not open and mouse cursor is close to left or right edge of SlidePanelsGrid
      MouseMove += delegate(object sender, MouseEventArgs e) {
        var pos = e.GetPosition(this);
        if (pos.X < 5 && ContentLeft != null && !ContentLeft.IsOpen)
          ContentLeft.IsOpen = true;
        if (pos.X > ActualWidth - 5 && ContentRight != null && !ContentRight.IsOpen)
          ContentRight.IsOpen = true;
      };

      if (Template.FindName("PART_MainGrid", this) is Grid mainGrid) {
        _mainGrid = mainGrid;
      }

      if (Template.FindName("PART_GridSplitterLeft", this) is GridSplitter gsLeft) {
        gsLeft.DragDelta += delegate { UpdateContentLeftWidth(); };
        gsLeft.DragCompleted += delegate { UpdateContentLeftWidth(); };
      }

      if (Template.FindName("PART_GridSplitterRight", this) is GridSplitter gsRight) {
        gsRight.DragDelta += delegate { UpdateContentRightWidth(); };
        gsRight.DragCompleted += delegate { UpdateContentRightWidth(); };
      }

      if (ContentLeft != null) {
        _mainGrid.ColumnDefinitions[0].Width = new GridLength(ContentLeft.Width);
        _mainGrid.ColumnDefinitions[1].Width = new GridLength(GridSplitterWidth);

        ContentLeft.OnIsPinnedChanged += delegate {
          _mainGrid.ColumnDefinitions[0].Width = new GridLength(ContentLeft.IsPinned ? ContentLeft.Width : 0);
          _mainGrid.ColumnDefinitions[1].Width = new GridLength(ContentLeft.IsPinned ? GridSplitterWidth : 0);
        };
      }

      if (ContentRight != null) {
        _mainGrid.ColumnDefinitions[3].Width = new GridLength(GridSplitterWidth);
        _mainGrid.ColumnDefinitions[4].Width = new GridLength(ContentRight.Width);

        ContentRight.OnIsPinnedChanged += delegate {
          _mainGrid.ColumnDefinitions[3].Width = new GridLength(ContentRight.IsPinned ? GridSplitterWidth : 0);
          _mainGrid.ColumnDefinitions[4].Width = new GridLength(ContentRight.IsPinned ? ContentRight.Width : 0);
        };
      }
    }

    private void UpdateContentLeftWidth() {
      if (ContentLeft == null) return;
      ContentLeft.Width = _mainGrid.ColumnDefinitions[0].ActualWidth;
      ContentLeft.UpdateAnimation();
      OnContentLeftWidthChanged?.Invoke();
    }

    private void UpdateContentRightWidth() {
      if (ContentRight == null) return;
      ContentRight.Width = _mainGrid.ColumnDefinitions[4].ActualWidth;
      ContentRight.UpdateAnimation();
      OnContentRightWidthChanged?.Invoke();
    }

  }
}
