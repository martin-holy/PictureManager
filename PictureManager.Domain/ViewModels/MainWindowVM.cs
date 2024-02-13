using MH.UI.Controls;
using MH.Utils.BaseClasses;
using PictureManager.Domain.DataViews;
using PictureManager.Domain.Models;

namespace PictureManager.Domain.ViewModels;

public class MainWindowVM : ObservableObject {
  private bool _isFullScreen;
  private bool _isInViewMode;

  public ToolBarVM ToolBar { get; } = new();
  public MiddleContentVM MiddleContent { get; } = new();
  public StatusBarVM StatusBar { get; }
  public ToolsTabsVM ToolsTabs { get; } = new() { CanCloseTabs = true };
  public SlidePanelsGrid SlidePanelsGrid { get; }

  public bool IsFullScreen {
    get => _isFullScreen;
    set {
      if (_isFullScreen == value) return;
      _isFullScreen = value;
      SlidePanelsGrid.PinLayouts[SlidePanelsGrid.ActiveLayout][4] = value;
      OnPropertyChanged();
    }
  }

  public bool IsInViewMode {
    get => _isInViewMode;
    set {
      _isInViewMode = value;
      SlidePanelsGrid.ActiveLayout = value ? 1 : 0;
      IsFullScreen = SlidePanelsGrid.PinLayouts[SlidePanelsGrid.ActiveLayout][4];
      OnPropertyChanged();
    }
  }

  public RelayCommand SwitchToBrowserCommand { get; }

  public MainWindowVM() {
    StatusBar = new(Core.Inst);
    SlidePanelsGrid = new(
      new(Position.Left, Core.TreeViewCategoriesM, 380),
      new(Position.Top, ToolBar, 30),
      new(Position.Right, ToolsTabs, GetToolsTabsWidth()) { CanOpen = false },
      new(Position.Bottom, StatusBar, 0),
      MiddleContent,
      new[] { // Left, Top, Right, Bottom, FullScreen (not part of SlidePanelsGrid)
        new[] { true, true, false, true, false }, // browse mode
        new[] { false, false, false, true, false } // view mode
      });
    
    SwitchToBrowserCommand = new(() => IsInViewMode = false, () => Core.MediaViewerM.IsVisible);
    AttachEvents();
  }

  private void AttachEvents() {
    ToolsTabs.TabClosedEvent += (_, e) => {
      switch (e.Data.Data) {
        case PersonDetail pd: pd.Reload(null); break;
      }
    };

    ToolsTabs.Tabs.CollectionChanged += (_, e) => {
      SlidePanelsGrid.PanelRight.CanOpen = ToolsTabs.Tabs.Count > 0;
      if (e.NewItems != null)
        SlidePanelsGrid.PanelRight.IsOpen = true;
    };
  }

  // (segment size + 1) * count + ScrollBar + Margin + ToBeSure
  private static double GetToolsTabsWidth() =>
    (SegmentsM.SegmentUiFullWidth + 1) * 4 + 15 + 2 + 1;
}