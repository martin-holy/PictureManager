using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MH.UI.WPF.Controls {
  public class SlidePanelsGrid : Control {
    public static readonly DependencyProperty ActiveLayoutProperty =
      DependencyProperty.Register(nameof(ActiveLayout), typeof(int), typeof(SlidePanelsGrid), new(ActiveLayoutChanged));

    public static readonly DependencyProperty PinLayoutsProperty =
      DependencyProperty.Register(nameof(PinLayouts), typeof(object[]), typeof(SlidePanelsGrid));

    public static readonly DependencyProperty PanelLeftProperty =
      DependencyProperty.Register(nameof(PanelLeft), typeof(SlidePanel), typeof(SlidePanelsGrid));

    public static readonly DependencyProperty PanelTopProperty =
      DependencyProperty.Register(nameof(PanelTop), typeof(SlidePanel), typeof(SlidePanelsGrid));

    public static readonly DependencyProperty PanelRightProperty =
      DependencyProperty.Register(nameof(PanelRight), typeof(SlidePanel), typeof(SlidePanelsGrid));

    public static readonly DependencyProperty PanelBottomProperty =
      DependencyProperty.Register(nameof(PanelBottom), typeof(SlidePanel), typeof(SlidePanelsGrid));

    public static readonly DependencyProperty PanelMiddleProperty =
      DependencyProperty.Register(nameof(PanelMiddle), typeof(FrameworkElement), typeof(SlidePanelsGrid));
    
    public static readonly DependencyProperty PanelLeftGridWidthProperty =
      DependencyProperty.Register(nameof(PanelLeftGridWidth), typeof(GridLength), typeof(SlidePanelsGrid), new(PanelLeftGridWidthChanged));

    public static readonly DependencyProperty PanelTopGridHeightProperty =
      DependencyProperty.Register(nameof(PanelTopGridHeight), typeof(GridLength), typeof(SlidePanelsGrid));

    public static readonly DependencyProperty PanelRightGridWidthProperty =
      DependencyProperty.Register(nameof(PanelRightGridWidth), typeof(GridLength), typeof(SlidePanelsGrid), new(PanelRightGridWidthChanged));

    public static readonly DependencyProperty PanelBottomGridHeightProperty =
      DependencyProperty.Register(nameof(PanelBottomGridHeight), typeof(GridLength), typeof(SlidePanelsGrid));

    public int ActiveLayout {
      get => (int)GetValue(ActiveLayoutProperty);
      set => SetValue(ActiveLayoutProperty, value);
    }

    public object[] PinLayouts {
      get => (object[])GetValue(PinLayoutsProperty);
      set => SetValue(PinLayoutsProperty, value);
    }

    public SlidePanel PanelLeft {
      get => (SlidePanel)GetValue(PanelLeftProperty);
      set => SetValue(PanelLeftProperty, value);
    }

    public SlidePanel PanelTop {
      get => (SlidePanel)GetValue(PanelTopProperty);
      set => SetValue(PanelTopProperty, value);
    }

    public SlidePanel PanelRight {
      get => (SlidePanel)GetValue(PanelRightProperty);
      set => SetValue(PanelRightProperty, value);
    }

    public SlidePanel PanelBottom {
      get => (SlidePanel)GetValue(PanelBottomProperty);
      set => SetValue(PanelBottomProperty, value);
    }

    public FrameworkElement PanelMiddle {
      get => (FrameworkElement)GetValue(PanelMiddleProperty);
      set => SetValue(PanelMiddleProperty, value);
    }

    public GridLength PanelLeftGridWidth {
      get => (GridLength)GetValue(PanelLeftGridWidthProperty);
      set => SetValue(PanelLeftGridWidthProperty, value);
    }

    public GridLength PanelTopGridHeight {
      get => (GridLength)GetValue(PanelTopGridHeightProperty);
      set => SetValue(PanelTopGridHeightProperty, value);
    }

    public GridLength PanelRightGridWidth {
      get => (GridLength)GetValue(PanelRightGridWidthProperty);
      set => SetValue(PanelRightGridWidthProperty, value);
    }

    public GridLength PanelBottomGridHeight {
      get => (GridLength)GetValue(PanelBottomGridHeightProperty);
      set => SetValue(PanelBottomGridHeightProperty, value);
    }

    private GridSplitter _gridSplitterLeft;
    private GridSplitter _gridSplitterRight;

    static SlidePanelsGrid() {
      DefaultStyleKeyProperty.OverrideMetadata(
        typeof(SlidePanelsGrid),
        new FrameworkPropertyMetadata(typeof(SlidePanelsGrid)));
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();
      MouseMove += OnMouseMove;

      _gridSplitterLeft = Template.FindName("PART_GridSplitterLeft", this) as GridSplitter;
      _gridSplitterRight = Template.FindName("PART_GridSplitterRight", this) as GridSplitter;

      ActivateLayout(0);
      InitPanel(PanelLeft, Dock.Left);
      InitPanel(PanelTop, Dock.Top);
      InitPanel(PanelRight, Dock.Right);
      InitPanel(PanelBottom, Dock.Bottom);
    }

    private void InitPanel(SlidePanel panel, Dock position) {
      if (panel == null) return;

      panel.Position = position;
      panel.SizeChanged += delegate { SetPin(panel); };
      panel.IsPinnedChangedEventHandler += delegate {
        ((bool[])PinLayouts[ActiveLayout])[(int)panel.Position] = panel.IsPinned;
        SetPin(panel);
      };
    }

    private static void ActiveLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
      (d as SlidePanelsGrid)?.ActivateLayout((int)e.NewValue);

    public void ActivateLayout(int idx) {
      var activeLayout = (bool[])PinLayouts[idx];
      if (PanelLeft != null) PanelLeft.IsPinned = activeLayout[0];
      if (PanelTop != null) PanelTop.IsPinned = activeLayout[1];
      if (PanelRight != null) PanelRight.IsPinned = activeLayout[2];
      if (PanelBottom != null) PanelBottom.IsPinned = activeLayout[3];
    }

    private static void PanelLeftGridWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
      (d as SlidePanelsGrid)?.PanelLeft?.SetWidth(((GridLength)e.NewValue).Value);

    private static void PanelRightGridWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
      (d as SlidePanelsGrid)?.PanelRight?.SetWidth(((GridLength)e.NewValue).Value);

    private void SetPin(SlidePanel panel) {
      if (panel == null) return;

      if (panel.Position == Dock.Top || panel.Position == Dock.Bottom) {
        var height = new GridLength(panel.IsPinned && panel.CanOpen ? panel.ActualHeight : 0);

        if (panel.Position == Dock.Top && PanelTopGridHeight != height)
          PanelTopGridHeight = height;
        else if (panel.Position == Dock.Bottom && PanelBottomGridHeight != height)
          PanelBottomGridHeight = height;
      }
      else {
        var visibility = panel.IsPinned && panel.CanOpen ? Visibility.Visible : Visibility.Collapsed;
        var width = new GridLength(panel.IsPinned && panel.CanOpen ? panel.ActualWidth : 0);

        if (panel.Position == Dock.Left) {
          if (PanelLeftGridWidth != width)
            PanelLeftGridWidth = width;

          if (_gridSplitterLeft.Visibility != visibility)
            _gridSplitterLeft.Visibility = visibility;
        }
        else if (panel.Position == Dock.Right) {
          if (PanelRightGridWidth != width)
            PanelRightGridWidth = width;

          if (_gridSplitterRight.Visibility != visibility)
            _gridSplitterRight.Visibility = visibility;
        }
      }
    }

    private void OnMouseMove(object sender, MouseEventArgs e) {
      var pos = e.GetPosition(this);

      // to stop opening/closing panel by it self in some cases
      if ((pos.X == 0 && pos.Y == 0) || pos.X < 0 || pos.Y < 0) return;

      if (PanelLeft != null) {
        if (PanelLeft.IsOpen) {
          if (!PanelLeft.IsPinned && pos.X > PanelLeft.ActualWidth)
            PanelLeft.IsOpen = false;
        }
        else if (pos.X < 5 && PanelLeft.CanOpen)
          PanelLeft.IsOpen = true;
      }

      if (PanelRight != null) {
        if (PanelRight.IsOpen) {
          if (!PanelRight.IsPinned && pos.X < (ActualWidth - PanelRight.ActualWidth))
            PanelRight.IsOpen = false;
        }
        else if (pos.X > ActualWidth - 5 && PanelRight.CanOpen)
          PanelRight.IsOpen = true;
      }

      if (PanelTop != null) {
        if (PanelTop.IsOpen) {
          if (!PanelTop.IsPinned && pos.Y > PanelTop.ActualHeight)
            PanelTop.IsOpen = false;
        }
        else if (pos.Y < 5 && PanelTop.CanOpen)
          PanelTop.IsOpen = true;
      }

      if (PanelBottom != null) {
        if (PanelBottom.IsOpen) {
          if (!PanelBottom.IsPinned && pos.Y < (ActualHeight - PanelBottom.ActualHeight))
            PanelBottom.IsOpen = false;
        }
        else if (pos.Y > ActualHeight - 5 && PanelBottom.CanOpen)
          PanelBottom.IsOpen = true;
      }
    }
  }
}
