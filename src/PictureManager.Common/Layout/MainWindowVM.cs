using MH.UI.Controls;
using MH.Utils.BaseClasses;
using PictureManager.Common.Features.Common;
using PictureManager.Common.Features.MediaItem.Video;
using PictureManager.Common.Features.Person;

namespace PictureManager.Common.Layout;

public class MainWindowVM : ObservableObject {
  private readonly CoreVM _coreVM;
  private bool _isFullScreen;
  private bool _isInViewMode;

  public MiddleContentVM MiddleContent { get; } = new();
  public SlidePanelsGrid SlidePanelsGrid { get; }
  public StatusBarVM StatusBar { get; }
  public ToolBarVM ToolBar { get; } = new();
  public ToolsTabsVM ToolsTabs { get; } = new() { CanCloseTabs = true };
  public TreeViewCategoriesVM TreeViewCategories { get; } = new();
  public MainMenuVM MainMenu { get; } = new();

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
      if (_isInViewMode == value) return;
      _isInViewMode = value;
      SlidePanelsGrid.ActiveLayout = value ? 1 : 0;
      IsFullScreen = SlidePanelsGrid.PinLayouts[SlidePanelsGrid.ActiveLayout][4];
      StatusBar.Update(_coreVM.MediaItem.Current);
      OnPropertyChanged();
    }
  }

  public static RelayCommand SwitchToBrowserCommand { get; set; } = null!;

  public MainWindowVM(CoreVM coreVM) {
    _coreVM = coreVM;
    StatusBar = new(coreVM);
    SlidePanelsGrid = new(
      new(Dock.Left, TreeViewCategories),
      new(Dock.Top, ToolBar),
      new(Dock.Right, ToolsTabs) { CanOpen = false },
      new(Dock.Bottom, StatusBar),
      MiddleContent,
      [ // Left, Top, Right, Bottom, FullScreen (FullScreen is not part of SlidePanelsGrid)
        [true, true, false, true, false], // browse mode
        [false, false, false, true, false] // view mode
      ]);
    
    SwitchToBrowserCommand = new(() => IsInViewMode = false, () => IsInViewMode);
    AttachEvents();
  }

  public bool IsVideoPlayerVisible() =>
    IsInViewMode || (SlidePanelsGrid.PanelRight.IsPinned && ToolsTabs.Selected?.Data is VideoVM);

  private void AttachEvents() {
    ToolsTabs.TabActivatedEvent += (_, tab) => {
      if (tab.Data is not VideoVM)
        SlidePanelsGrid.PanelRight!.IsOpen = true;
    };

    ToolsTabs.TabClosedEvent += (_, tab) => {
      switch (tab.Data) {
        case PersonDetailVM pd: pd.Reload(null); break;
      }
    };

    ToolsTabs.Tabs.CollectionChanged += (_, e) =>
      SlidePanelsGrid.PanelRight!.CanOpen = ToolsTabs.Tabs.Count > 0;
  }
}