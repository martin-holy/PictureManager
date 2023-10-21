using MH.UI.Controls;
using MH.Utils.BaseClasses;
using PictureManager.Domain.CollectionViews;
using PictureManager.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using static MH.Utils.DragDropHelper;

namespace PictureManager.Domain.DataViews; 

public sealed class PersonDetail : ObservableObject {
  private readonly PeopleM _peopleM;
  private readonly SegmentsM _segmentsM;
  private PersonM _personM;

  public CollectionViewSegments AllSegments { get; } = new();
  public CollectionViewSegments TopSegments { get; } = new();
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
    _peopleM.ToggleTopSegment(PersonM, data as SegmentM);
    ReloadTopSegments();
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
      GroupByItems.GetKeywordsInGroupFromSegments(items)
    };

    AllSegments.Reload(source, GroupMode.ThenByRecursive, groupByItems, true, "All");
  }

  private void ReloadTopSegments() =>
    TopSegments.Reload(
      PersonM.TopSegments == null
        ? new()
        : PersonM.TopSegments.Cast<SegmentM>().ToList(),
      GroupMode.GroupBy, null, true, "Top", false);

  public void ReloadIf(IEnumerable<SegmentM> segments, IEnumerable<PersonM> people) {
    if (people.Contains(PersonM))
      Reload(PersonM);
    else
      ReGroupIfContains(segments, false);
  }

  public void ReGroupIfContains(IEnumerable<SegmentM> segments, bool remove) {
    if (PersonM == null) return;
    var items = segments.Where(x => ReferenceEquals(PersonM, x.Person)).ToArray();
    if (items.Length == 0) return;
    AllSegments.ReGroupItems(items, remove);
    if (PersonM.TopSegments == null) return;
    items = items.Where(PersonM.TopSegments.Contains).ToArray();
    if (items.Length == 0) return;
    TopSegments.ReGroupItems(items, remove);
  }
}