using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.Controls;
using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Converters;
using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.CustomControls;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Utils;
using ObservableObject = MH.Utils.BaseClasses.ObservableObject;

namespace PictureManager.ViewModels {
  public sealed class SegmentsVM : ObservableObject {
    private readonly Core _core;
    private readonly AppCore _coreVM;
    private bool _matchingAutoSort;

    private readonly WorkTask _workTask = new();
    private readonly IProgress<int> _progress;
    private readonly int _segmentPanelWidth = 100 + 6; //border, margin, padding, ... //TODO find the real value
    private List<MediaItemM> _mediaItems;

    public SegmentsM SegmentsM { get; set; }
    public SegmentsDrawerVM SegmentsDrawerVM { get; }
    public SegmentsRectsVM SegmentsRectsVM { get; }
    public bool MatchingAutoSort { get => _matchingAutoSort; set { _matchingAutoSort = value; OnPropertyChanged(); } }
    public VirtualizingWrapPanel MatchingPanel { get; set; }
    public VirtualizingWrapPanel ConfirmedMatchingPanel { get; set; }
    public HeaderedListItem<object, string> MainTabsItem { get; }
    
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
    public RelayCommand<SizeChangedEventArgs> MatchingPanelSizeChangedCommand { get; }
    public RelayCommand<SizeChangedEventArgs> ConfirmedMatchingPanelSizeChangedCommand { get; }

    public SegmentsVM(Core core, AppCore coreVM, SegmentsM segmentsM) {
      _core = core;
      _coreVM = coreVM;
      SegmentsM = segmentsM;

      MainTabsItem = new(this, "Segment Matching");
      SegmentsDrawerVM = new(SegmentsM);
      SegmentsRectsVM = new(segmentsM.SegmentsRectsM);

      MatchingPanel = new();
      ConfirmedMatchingPanel = new();

      MatchingPanel.Style = (Style)Application.Current.FindResource("Views.SegmentsV.MatchingPanel");
      ConfirmedMatchingPanel.Style = (Style)Application.Current.FindResource("Views.SegmentsV.ConfirmedMatchingPanel");

      DragDropFactory.SetDrag(MatchingPanel, CanDrag);
      DragDropFactory.SetDrag(ConfirmedMatchingPanel, CanDrag);

      // TODO move _progress to Model
      _progress = new Progress<int>(x => {
        _core.TitleProgressBarM.ValueA = x;
        _core.TitleProgressBarM.ValueB = x;
      });

      SetSelectedAsSamePersonCommand = new(SegmentsM.SetSelectedAsSamePerson);
      SetSelectedAsUnknownCommand = new(SegmentsM.SetSelectedAsUnknown);
      SegmentToolTipReloadCommand = new(SegmentsM.SegmentToolTipReload);
      ViewMediaItemsWithSegmentCommand = new(ViewMediaItemsWithSegment);
      SelectCommand = new(Select);
      SegmentMatchingCommand = new(SegmentMatching, () => _core.ThumbnailsGridsM.Current?.FilteredItems.Count > 0);
      GroupConfirmedCommand = new(() => Reload(false, true));
      CompareAllGroupsCommand = new(() => LoadSegments(true));
      SortCommand = new(() => SortAndReload(true, true));
      CompareCommand = new(async () => {
        await CompareAsync();
        SortAndReload(true, true);
      });
      OpenSegmentsDrawerCommand = new(() => App.Ui.ToolsTabsVM.Activate(SegmentsDrawerVM.ToolsTabsItem, true));
      GroupMatchingPanelCommand = new(() => Reload(true, false));

      MatchingPanelSizeChangedCommand = new(
        () => Reload(true, false),
        e => e.WidthChanged && !_coreVM.MainWindowVM.IsFullScreenIsChanging);
      ConfirmedMatchingPanelSizeChangedCommand = new(
        () => Reload(false, true),
        e => e.WidthChanged && !_coreVM.MainWindowVM.IsFullScreenIsChanging);

      // TODO do it just when needed
      foreach (var person in App.Core.PeopleM.All)
        person.UpdateDisplayKeywords();
    }

    private static object CanDrag(MouseEventArgs e) =>
      (e.OriginalSource as FrameworkElement)?.DataContext as SegmentM;

    private void ViewMediaItemsWithSegment(SegmentM segmentM) {
      var items = SegmentsM.GetMediaItemsWithSegment(segmentM, _coreVM.MainTabsVM.Selected == MainTabsItem);
      if (items == null) return;

      _coreVM.MediaViewerVM.SetMediaItems(items, segmentM.MediaItem);
      _coreVM.MainWindowVM.IsFullScreen = true;
    }

    private void Select(ClickEventArgs e) {
      if (e.OriginalSource is Image { DataContext: SegmentM segmentM } image) {
        var list = image.TryFindParent<StackPanel>()
          ?.DataContext is VirtualizingWrapPanelRow { Group: { } } row
            ? row.Group.Items.Cast<SegmentM>().ToList()
            : new() { segmentM };
        SegmentsM.Select(list, segmentM, e.IsCtrlOn, e.IsShiftOn);
      }
    }

    public async Task CompareAsync() {
      await _workTask.Cancel();
      SegmentsM.AddSegmentsForComparison();
      _core.TitleProgressBarM.ResetProgressBars(SegmentsM.Loaded.Count);
      await _workTask.Start(SegmentsM.FindSimilaritiesAsync(SegmentsM.Loaded, _progress, _workTask.Token));
    }

    private void SegmentMatching() {
      // TODO do I need _mediaItems prop??
      _mediaItems = _core.ThumbnailsGridsM.Current.GetSelectedOrAll();
      _coreVM.MainTabsVM.Activate(MainTabsItem);

      var all = MessageDialog.Show(
        "Segment Matching",
        "Do you want to load all segments or just segments with person?",
        true,
        new[] { "All segments", "Segments with person" });

      LoadSegments(!all);
    }

    public void LoadSegments(bool withPersonOnly) {
      MatchingPanel.ClearRows();
      MatchingPanel.AddGroup("IconPeople", "?");
      ConfirmedMatchingPanel.ClearRows();
      SegmentsM.GroupSegments = false;
      SegmentsM.ResetBeforeNewLoad();

      foreach (var segment in SegmentsM.GetSegments(_mediaItems, withPersonOnly)) {
        SegmentsM.Loaded.Add(segment);
        MatchingPanel.AddItem(segment, _segmentPanelWidth);
      }

      SortAndReload(false, true);
    }

    public void SortAndReload() =>
      SortAndReload(MatchingAutoSort, MatchingAutoSort);

    public void SortAndReload(bool segments, bool confirmedSegments) {
      Sort(segments, confirmedSegments);
      Reload(segments, confirmedSegments);
    }

    private void Sort(bool segments, bool confirmedSegments) {
      if (segments) SegmentsM.ReloadLoadedGroupedByPerson();
      if (confirmedSegments) SegmentsM.ReloadConfirmedSegments();
    }

    public void Reload(bool segments, bool confirmedSegments) {
      if (segments) ReloadMatchingPanel();
      if (confirmedSegments) ReloadConfirmedMatchingPanel();
    }

    private void ReloadMatchingPanel() {
      var rowIndex = MatchingPanel.GetTopRowIndex();
      var itemToScrollTo = MatchingPanel.GetFirstItemFromRow(rowIndex);
      MatchingPanel.ClearRows();
      // TODO UpdateLayout repeats it self. find the way to do it without everywhere
      //MatchingPanel.UpdateLayout();
      MatchingPanel.UpdateMaxRowWidth();

      if (SegmentsM.GroupSegments) {
        if (SegmentsM.LoadedGroupedByPerson.Count == 0)
          SegmentsM.ReloadLoadedGroupedByPerson();

        foreach (var group in SegmentsM.LoadedGroupedByPerson) {
          var groupTitle = group[0].Person != null
            ? group[0].Person.Name
            : group[0].PersonId.ToString();
          MatchingPanel.AddGroup("IconPeople", groupTitle);

          foreach (var segment in group)
            MatchingPanel.AddItem(segment, _segmentPanelWidth);
        }
      }
      else {
        MatchingPanel.AddGroup("IconPeople", "?");
        foreach (var segment in SegmentsM.Loaded)
          MatchingPanel.AddItem(segment, _segmentPanelWidth);
      }

      if (rowIndex > 0)
        MatchingPanel.ScrollTo(itemToScrollTo);
    }

    private void ReloadConfirmedMatchingPanel() {
      var itemToScrollTo = ConfirmedMatchingPanel.GetFirstItemFromRow(ConfirmedMatchingPanel.GetTopRowIndex());
      ConfirmedMatchingPanel.ClearRows();
      //ConfirmedMatchingPanel.UpdateLayout();
      ConfirmedMatchingPanel.UpdateMaxRowWidth();

      if (SegmentsM.GroupConfirmedSegments) {
        foreach (var (personId, segment, similar) in SegmentsM.ConfirmedSegments) {
          var groupTitle = segment.Person != null
            ? segment.Person.Name
            : personId.ToString();
          ConfirmedMatchingPanel.AddGroup("IconPeople", groupTitle);
          ConfirmedMatchingPanel.AddItem(segment, _segmentPanelWidth);

          foreach (var simGroup in similar.OrderByDescending(x => x.sim))
            ConfirmedMatchingPanel.AddItem(simGroup.segment, _segmentPanelWidth);
        }
      }
      else {
        foreach (var group in SegmentsM.ConfirmedSegments
          .GroupBy(x => {
            if (x.segment.Person == null) return "Unknown";
            return x.segment.Person.DisplayKeywords == null
              ? string.Empty
              : string.Join(", ", x.segment.Person.DisplayKeywords.Select(k => k.Name));
          })
          .OrderBy(g => g.First().personId < 0)
          .ThenBy(g => g.Key)) {

          // add group
          if (!string.IsNullOrEmpty(group.Key) && !"Unknown".Equals(group.Key))
            ConfirmedMatchingPanel.AddGroup("IconTag", group.Key);
          if ("Unknown".Equals(group.Key))
            ConfirmedMatchingPanel.AddGroup("IconPeople", group.Key);

          // add people
          foreach (var (_, segment, _) in group)
            ConfirmedMatchingPanel.AddItem(segment, _segmentPanelWidth);
        }
      }

      //TODO this is causing error
      //ConfirmedMatchingPanel.ScrollTo(itemToScrollTo);
    }
  }
}
