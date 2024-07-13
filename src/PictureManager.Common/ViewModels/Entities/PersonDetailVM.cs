using MH.UI.Controls;
using MH.Utils.BaseClasses;
using PictureManager.Common.CollectionViews;
using PictureManager.Common.Models;
using PictureManager.Common.Services;
using System.Collections.Generic;
using System.Linq;
using static MH.Utils.DragDropHelper;

namespace PictureManager.Common.ViewModels.Entities;

public sealed class PersonDetailVM : ObservableObject {
  private readonly PersonS _personS;
  private readonly SegmentS _segmentS;
  private PersonM? _personM;

  public CollectionViewSegments AllSegments { get; } = new();
  public CollectionViewSegments TopSegments { get; } = new() { AddInOrder = false };
  public PersonM? PersonM { get => _personM; set { _personM = value; OnPropertyChanged(); } }
  public CanDropFunc CanDropFunc { get; }
  public DoDropAction TopSegmentsDropAction { get; }

  public PersonDetailVM(PersonS personS, SegmentS segmentS) {
    _personS = personS;
    _segmentS = segmentS;
    CanDropFunc = CanDrop;
    TopSegmentsDropAction = TopSegmentsDrop;
  }

  private MH.Utils.DragDropEffects CanDrop(object? target, object? data, bool haveSameOrigin) {
    if (!haveSameOrigin && data is SegmentM segment && PersonM != null && PersonM.TopSegments?.Contains(segment) != true)
      return MH.Utils.DragDropEffects.Copy;
    if (haveSameOrigin && data != target)
      return MH.Utils.DragDropEffects.Move;

    return MH.Utils.DragDropEffects.None;
  }

  private void TopSegmentsDrop(object data, bool haveSameOrigin) {
    var segment = (SegmentM)data;
    _personS.ToggleTopSegment(PersonM!, segment);
    if (haveSameOrigin) TopSegments.Remove(segment);
    else TopSegments.Insert(segment);
  }

  public void Reload(PersonM? person) {
    PersonM = person;

    if (PersonM == null) {
      AllSegments.Root?.Clear();
      TopSegments.Root?.Clear();
      return;
    }

    ReloadAllSegments(_segmentS.DataAdapter.GetBy(PersonM).ToList());
    ReloadTopSegments();
  }

  private void ReloadAllSegments(IReadOnlyCollection<SegmentM> items) {
    var source = items
      .OrderBy(x => x.MediaItem.FileName)
      .ToList();
    var groupByItems = new[] {
      GroupByItems.GetKeywordsInGroup(items)
    };

    AllSegments.Reload(source, GroupMode.ThenByRecursive, groupByItems, true, "All");
  }

  private void ReloadTopSegments() =>
    TopSegments.Reload(
      PersonM!.TopSegments == null
        ? []
        : PersonM.TopSegments.ToList(),
      GroupMode.GroupBy, null, true, "Top");

  public void Update(SegmentM[] segments) {
    Update(segments, true, false);
    Update(segments, false, true);
  }

  public void Update(SegmentM[] segments, bool where, bool remove) {
    if (PersonM == null) return;
    var items = segments.Where(x => ReferenceEquals(PersonM, x.Person) == where).ToArray();
    if (remove) AllSegments.Remove(items); else AllSegments.Insert(items);

    items = remove
      ? items
      : PersonM.TopSegments == null
        ? []
        : items.Where(PersonM.TopSegments.Contains).ToArray();

    if (remove) TopSegments.Remove(items); else TopSegments.Insert(items);
  }

  public void UpdateDisplayKeywordsIfContains(PersonM[] items) {
    if (PersonM != null && items.Contains(PersonM))
      PersonM.OnPropertyChanged(nameof(PersonM.DisplayKeywords));
  }
}