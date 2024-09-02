using MH.UI.Controls;
using MH.Utils.BaseClasses;
using PictureManager.Common.Features.Common;
using PictureManager.Common.Features.Person;
using PictureManager.Common.Features.Segment;

namespace PictureManager.Common.Layout;

public class MainWindowVM : ObservableObject {
  private bool _isFullScreen;
  private bool _isInViewMode;

  public MiddleContentVM MiddleContent { get; } = new();
  public SlidePanelsGrid SlidePanelsGrid { get; }
  public StatusBarVM StatusBar { get; }
  public ToolBarVM ToolBar { get; } = new();
  public ToolsTabsVM ToolsTabs { get; } = new() { CanCloseTabs = true };
  public TreeViewCategoriesVM TreeViewCategories { get; } = new();

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

  public static RelayCommand SwitchToBrowserCommand { get; set; } = null!;

  public MainWindowVM() {
    StatusBar = new(Core.Inst);
    SlidePanelsGrid = new(
      new(Dock.Left, TreeViewCategories, 380),
      new(Dock.Top, ToolBar, 30),
      new(Dock.Right, ToolsTabs, GetToolsTabsWidth(SegmentVM.SegmentUiFullWidth)) { CanOpen = false },
      new(Dock.Bottom, StatusBar, 0),
      MiddleContent,
      [ // Left, Top, Right, Bottom, FullScreen (not part of SlidePanelsGrid)
        [true, true, false, true, false], // browse mode
        [false, false, false, true, false] // view mode
      ]);
    
    SwitchToBrowserCommand = new(() => IsInViewMode = false, () => Core.VM.MediaViewer.IsVisible);
    AttachEvents();
  }

  private void AttachEvents() {
    ToolsTabs.TabActivatedEvent += _ => {
      SlidePanelsGrid.PanelRight!.IsOpen = true;
    };

    ToolsTabs.TabClosedEvent += tab => {
      switch (tab.Data) {
        case PersonDetailVM pd: pd.Reload(null); break;
      }
    };

    ToolsTabs.Tabs.CollectionChanged += (_, e) => {
      SlidePanelsGrid.PanelRight!.CanOpen = ToolsTabs.Tabs.Count > 0;
      if (e.NewItems != null)
        SlidePanelsGrid.PanelRight.IsOpen = true;
    };
  }

  // (segment size + 1) * count + ScrollBar + Margin + ToBeSure
  private static double GetToolsTabsWidth(int segmentSize) =>
    (segmentSize + 1) * 4 + 15 + 2 + 1;
}