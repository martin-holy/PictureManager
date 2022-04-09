using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using MahApps.Metro.Controls;
using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Controls;
using MH.UI.WPF.Converters;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Utils;

namespace PictureManager.ViewModels {
  public sealed class SegmentsVM : ObservableObject {
    private readonly Core _core;
    private readonly AppCore _coreVM;
    private double _segmentUiSize;
    private double _confirmedPanelWidth;
    private VirtualizingWrapPanel _matchingPanel;
    private VirtualizingWrapPanel _confirmedMatchingPanel;

    private readonly WorkTask _workTask = new();
    private List<MediaItemM> _mediaItems;

    public SegmentsM SegmentsM { get; set; }
    public SegmentsDrawerVM SegmentsDrawerVM { get; }
    public SegmentsRectsVM SegmentsRectsVM { get; }
    public double ConfirmedPanelWidth { get => _confirmedPanelWidth; private set { _confirmedPanelWidth = value; OnPropertyChanged(); } }
    public HeaderedListItem<object, string> MainTabsItem { get; }
    public double SegmentUiFullWidth { get; set; }

    public double SegmentUiSize {
      get => _segmentUiSize;
      set {
        _segmentUiSize = value;
        SegmentUiFullWidth = value + 6; // + border, margin
        ConfirmedPanelWidth = (SegmentUiFullWidth * 2) + AppCore.ScrollBarSize;
        OnPropertyChanged();
      }
    }

    public RelayCommand<object> SetSelectedAsSamePersonCommand { get; }
    public RelayCommand<object> SetSelectedAsUnknownCommand { get; }
    public RelayCommand<SegmentM> SegmentToolTipReloadCommand { get; }
    public RelayCommand<SegmentM> ViewMediaItemsWithSegmentCommand { get; }
    public RelayCommand<ClickEventArgs> SelectCommand { get; }
    public RelayCommand<object> SegmentMatchingCommand { get; }
    public RelayCommand<object> GroupConfirmedCommand { get; }
    public RelayCommand<object> CompareAllGroupsCommand { get; }
    public RelayCommand<object> SortCommand { get; }
    public RelayCommand<object> CompareCommand { get; }
    public RelayCommand<object> OpenSegmentsDrawerCommand { get; }
    public RelayCommand<object> GroupMatchingPanelCommand { get; }
    public RelayCommand<RoutedEventArgs> MatchingPanelLoadedCommand { get; }
    public RelayCommand<RoutedEventArgs> ConfirmedMatchingPanelLoadedCommand { get; }
    public RelayCommand<SizeChangedEventArgs> MatchingPanelSizeChangedCommand { get; }
    public RelayCommand<DragCompletedEventArgs> ConfirmedMatchingPanelSizeChangedCommand { get; }

    public SegmentsVM(Core core, AppCore coreVM, SegmentsM segmentsM) {
      _core = core;
      _coreVM = coreVM;
      SegmentsM = segmentsM;

      MainTabsItem = new(this, "Segment Matching");
      SegmentsDrawerVM = new(SegmentsM);
      SegmentsRectsVM = new(segmentsM.SegmentsRectsM);

      SetSelectedAsSamePersonCommand = new(SegmentsM.SetSelectedAsSamePerson);
      SetSelectedAsUnknownCommand = new(SegmentsM.SetSelectedAsUnknown);
      SegmentToolTipReloadCommand = new(SegmentsM.SegmentToolTipReload);
      ViewMediaItemsWithSegmentCommand = new(ViewMediaItemsWithSegment);
      SelectCommand = new(Select);
      SegmentMatchingCommand = new(SegmentMatching, () => _core.ThumbnailsGridsM.Current?.FilteredItems.Count > 0);
      GroupConfirmedCommand = new(() => SegmentsM.Reload(false, true));
      CompareAllGroupsCommand = new(() => SegmentsM.LoadSegments(_mediaItems, 1));
      SortCommand = new(() => SegmentsM.Reload(true, true));
      CompareCommand = new(async () => {
        await CompareAsync();
        SegmentsM.Reload(true, true);
      });
      OpenSegmentsDrawerCommand = new(() => App.Ui.ToolsTabsVM.Activate(SegmentsDrawerVM.ToolsTabsItem, true));
      GroupMatchingPanelCommand = new(() => SegmentsM.Reload(true, false));

      MatchingPanelLoadedCommand = new(OnMatchingPanelLoaded);
      ConfirmedMatchingPanelLoadedCommand = new(OnConfirmedMatchingPanelLoaded);
      MatchingPanelSizeChangedCommand = new(
        () => _matchingPanel.ReWrap(),
        e => e.WidthChanged && !_coreVM.MainWindowVM.IsFullScreenIsChanging);
      ConfirmedMatchingPanelSizeChangedCommand = new(() => {
        _confirmedMatchingPanel.UpdateLayout();
        _confirmedMatchingPanel.ReWrap();
      });

      // TODO do it just when needed
      foreach (var person in App.Core.PeopleM.All)
        person.UpdateDisplayKeywords();
    }

    private void OnMatchingPanelLoaded(RoutedEventArgs e) {
      _matchingPanel = e.Source as VirtualizingWrapPanel;
      DragDropFactory.SetDrag(_matchingPanel, CanDrag);
    }

    private void OnConfirmedMatchingPanelLoaded(RoutedEventArgs e) {
      _confirmedMatchingPanel = e.Source as VirtualizingWrapPanel;
      DragDropFactory.SetDrag(_confirmedMatchingPanel, CanDrag);
    }

    private object CanDrag(MouseEventArgs e) =>
      e.OriginalSource is FrameworkElement { DataContext: SegmentM segmentM }
        ? SegmentsM.GetOneOrSelected(segmentM)
        : null;

    private void ViewMediaItemsWithSegment(SegmentM segmentM) {
      var items = SegmentsM.GetMediaItemsWithSegment(segmentM, _coreVM.MainTabsVM.Selected == MainTabsItem);
      if (items == null) return;

      _coreVM.MediaViewerVM.SetMediaItems(items, segmentM.MediaItem);
      _coreVM.MainWindowVM.IsFullScreen = true;
    }

    private void Select(ClickEventArgs e) {
      if (e.OriginalSource is Image { DataContext: SegmentM segmentM } image) {
        var list = image.TryFindParent<StackPanel>()
          ?.DataContext is VirtualizingWrapPanelRow { Items: { } } row
            ? row.Items.Cast<SegmentM>().ToList()
            : new() { segmentM };
        SegmentsM.Select(list, segmentM, e.IsCtrlOn, e.IsShiftOn);
      }
    }

    private async Task CompareAsync() {
      await _workTask.Cancel();
      SegmentsM.AddSegmentsForComparison();
      _core.TitleProgressBarM.ResetProgressBars(SegmentsM.Loaded.Count);
      await _workTask.Start(SegmentsM.FindSimilaritiesAsync(SegmentsM.Loaded, _workTask.Token));
    }

    private void SegmentMatching() {
      var result = Core.DialogHostShow(new MessageDialog(
        "Segment Matching",
        "Do you want to load all segments, segments with person \nor one segment from each person?",
        "IconQuestion",
        true,
        new[] { "All segments", "Segments with person", "One from each" }));

      if (result == -1) return;

      _mediaItems = _core.ThumbnailsGridsM.Current.GetSelectedOrAll();
      _coreVM.MainTabsVM.Activate(MainTabsItem);

      SegmentsM.LoadSegments(_mediaItems, result);
    }
  }
}
