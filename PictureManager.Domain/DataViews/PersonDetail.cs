using MH.UI.Controls;
using MH.Utils.BaseClasses;
using PictureManager.Domain.CollectionViews;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using static MH.Utils.DragDropHelper;

namespace PictureManager.Domain.DataViews;

public sealed class PersonDetail : ObservableObject {
  private readonly PeopleM _peopleM;
  private readonly SegmentsM _segmentsM;
  private PersonM _personM;

  public CollectionViewSegments AllSegments { get; } = new();
  public CollectionViewSegments TopSegments { get; } = new() { AddInOrder = false };
  public PersonM PersonM { get => _personM; set { _personM = value; OnPropertyChanged(); } }
  public CanDropFunc CanDropFunc { get; }
  public DoDropAction TopSegmentsDropAction { get; }

  public PersonDetail(PeopleM peopleM, SegmentsM segmentsM) {
    _peopleM = peopleM;
    _segmentsM = segmentsM;
    CanDropFunc = CanDrop;
    TopSegmentsDropAction = TopSegmentsDrop;
  }

  private MH.Utils.DragDropEffects CanDrop(object target, object data, bool haveSameOrigin) {
    if (!haveSameOrigin && PersonM.TopSegments?.Contains(data as SegmentM) != true)
      return MH.Utils.DragDropEffects.Copy;
    if (haveSameOrigin && data != target)
      return MH.Utils.DragDropEffects.Move;

    return MH.Utils.DragDropEffects.None;
  }

  private void TopSegmentsDrop(object data, bool haveSameOrigin) {
    var segment = data as SegmentM;
    _peopleM.ToggleTopSegment(PersonM, segment);
    TopSegments.ReGroupItems(new[] { segment }, haveSameOrigin);
  }

  public void Reload(PersonM person) {
    PersonM = person;

    if (PersonM == null) {
      AllSegments.Root?.Clear();
      TopSegments.Root?.Clear();
      return;
    }

    ReloadAllSegments(_segmentsM.DataAdapter.All
      .Where(x => ReferenceEquals(x.Person, PersonM))
      .ToList());

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
      PersonM.TopSegments == null
        ? new()
        : PersonM.TopSegments.ToList(),
      GroupMode.GroupBy, null, true, "Top");

  public void Update(SegmentM[] segments) {
    Update(segments, true, false);
    Update(segments, false, true);
  }

  public void Update(SegmentM[] segments, bool where, bool remove) {
    if (PersonM == null) return;
    var items = segments.Where(x => ReferenceEquals(PersonM, x.Person) == where).ToArray();
    AllSegments.ReGroupItems(items, remove, remove);

    items = remove
      ? items
      : PersonM.TopSegments == null
        ? Array.Empty<SegmentM>()
        : items.Where(PersonM.TopSegments.Contains).ToArray();

    TopSegments.ReGroupItems(items, remove, remove);
  }

  public void UpdateDisplayKeywordsIfContains(PersonM[] items) {
    if (PersonM != null && items.Contains(PersonM))
      PersonM.OnPropertyChanged(nameof(PersonM.DisplayKeywords));
  }
}