using MH.UI.Controls;
using MH.UI.Dialogs;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Common.Features.Common;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.Person;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace PictureManager.Common.Features.Segment;

public sealed class SegmentsViewsVM : ObservableObject {
  private readonly SegmentS _segmentS;
  private readonly Dictionary<SegmentsViewVM, SegmentM[]> _views = [];
  private SegmentsViewVM? _current;

  public SegmentsViewsTabsVM Tabs { get; } = new();
  public SegmentsViewVM? Current { get => _current; private set => _setCurrent(value); }
  public static RelayCommand AddViewCommand { get; set; } = null!;

  public SegmentsViewsVM(SegmentS segmentS) {
    _segmentS = segmentS;
    AddViewCommand = new(() => _addView(string.Empty), Res.IconPlus, "Add Segments View Tab");
    Tabs.PropertyChanged += _onTabsPropertyChanged;
    Tabs.TabClosedEvent += _onTabsTabClosed;
  }

  private void _setCurrent(SegmentsViewVM? value) {
    SegmentM[] oldSelected = [];
    SegmentM[] newSelected = [];

    if (_current != null) {
      oldSelected = _current.Root.Source.Where(x => x.IsSelected).ToArray();

      if (_views.ContainsKey(_current))
        _views[_current] = oldSelected;
    }

    _current = value;

    if (_current != null)
      newSelected = _views[_current];

    _segmentS.Selected.Set(oldSelected.Except(newSelected), false);
    _segmentS.Selected.Set(newSelected, true);
    _segmentS.OnPropertyChanged(nameof(SegmentS.CanSetAsSamePerson));

    OnPropertyChanged(nameof(Current));
  }

  private void _onTabsPropertyChanged(object? sender, PropertyChangedEventArgs e) {
    if (e.Is(nameof(Tabs.Selected)) && Tabs.Selected?.Data is SegmentsViewVM view)
      Current = view;
  }

  private void _onTabsTabClosed(object? sender, IListItem data) {
    if (data.Data is not SegmentsViewVM view) return;
    _views.Remove(view);
    if (!ReferenceEquals(view, Current)) return;
    Current = null;
  }

  public void RemoveSegments(IList<SegmentM> items) {
    foreach (var view in _views.Keys)
      view.RemoveSegments(items);
  }

  public void Close() {
    foreach (var tab in Tabs.Tabs.ToArray())
      Tabs.Close(tab);
  }

  private SegmentsViewVM _addViewIfNotActive(string tabName) {
    if (Tabs.Selected?.Data is not SegmentsViewVM view)
      return _addView(tabName);
    
    if (!string.IsNullOrEmpty(tabName))
      Tabs.Selected.Name += ", " + tabName;

    return view;
  }

  private SegmentsViewVM _addView(string tabName) {
    var view = new SegmentsViewVM();
    _views.Add(view, []);
    Current = view;
    Tabs.Add(Res.IconSegment, tabName, view);
    return view;
  }

  public void UpdateViews(SegmentM[] segments) {
    foreach (var view in _views.Keys)
      view.Update(segments);
  }

  public static int GetSegmentsToLoadUserInput() {
    var md = new MessageDialog("Segments", "Load segments from ...", Res.IconSegment, true);

    md.Buttons = [
      new(md.SetResult(1, MH.UI.Res.IconImage, "Media items"), true),
      new(md.SetResult(2, Res.IconPeople, "People")),
      new(md.SetResult(3, Res.IconSegment, "Segments"))
    ];

    return Dialog.Show(md);
  }

  public static IEnumerable<SegmentM> GetSegments(int mode) {
    switch (mode) {
      case 1:
        var items = Core.VM.MediaViewer.IsVisible
          ? Core.VM.MediaViewer.Current != null ? [Core.VM.MediaViewer.Current] : []
          : Core.VM.MediaItem.Views.Current?.GetSelectedOrAll().ToArray() ?? [];

        return items.Concat(items.GetVideoItems()).GetSegments();
      case 2:
        var people = Core.S.Person.Selected.Items.ToHashSet();

        return Core.R.Segment.All
          .Where(x => x.Person != null && people.Contains(x.Person))
          .OrderBy(x => x.MediaItem.FileName);
      case 3:
        return Core.S.Segment.Selected.Items;
      default:
        return [];
    }
  }

  public void OnSegmentsPersonChanged(SegmentM[] segments) {
    foreach (var view in _views.Keys)
      view.OnSegmentsPersonChanged(segments);
  }

  public void OnPersonsKeywordsChanged(PersonM[] items) {
    foreach (var view in _views.Keys)
      view.CvPeople.Update(items);
  }

  public void OnPersonDeleted(PersonM item) {
    foreach (var view in _views.Keys)
      view.CvPeople.Remove(item);
  }

  public void Load(SegmentM[] items, string tabTitle) {
    if (string.IsNullOrEmpty(tabTitle))
      tabTitle = $"Segments {_views.Count + 1}";

    var and = Keyboard.IsCtrlOn() && Current != null;
    var source = _sort(and ? items.Union(Current!.Root.Source) : items).ToList();
    var view = and ? _addViewIfNotActive(tabTitle) : _addView(tabTitle);
    var groupByItems = new[] {
      GroupByItems.GetPeopleInGroup(source),
      GroupByItems.GetKeywordsInGroup(source)
    };

    view.Reload(source, GroupMode.ThenByRecursive, groupByItems, true);
    view.ReloadPeople();
  }

  private static IEnumerable<SegmentM> _sort(IEnumerable<SegmentM> items) =>
    items.OrderBy(x => x.MediaItem.FileName);
}