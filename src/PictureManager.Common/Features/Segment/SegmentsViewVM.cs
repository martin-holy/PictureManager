using MH.UI.Controls;
using MH.Utils.Extensions;
using PictureManager.Common.Features.Person;
using System.Collections.Generic;
using System.Linq;
using static MH.Utils.DragDropHelper;

namespace PictureManager.Common.Features.Segment;

public sealed class SegmentsViewVM : SegmentCollectionView {
  public PersonCollectionView CvPeople { get; } = new();
  public CanDragFunc CanDragFunc { get; }

  public SegmentsViewVM() {
    CanDragFunc = one => _getOneOrSelected(one as SegmentM);
  }

  public override void OnIsVisibleChanged() {
    if (!IsVisible) return;
    ReGroupPendingItems();
    ScrollTo(TopGroup, TopItem);
  }

  public void OnSegmentsPersonChanged(SegmentM[] segments) {
    Insert(segments);
    var newP = Root?.Source.GetPeople().ToArray()!;
    var oldP = CvPeople.Root?.Source.EmptyIfNull().ToArray()!;
    var pIn = newP.Except(oldP).ToArray();
    var pOut = oldP.Except(newP).ToArray();
    CvPeople.Insert(pIn);
    CvPeople.Remove(pOut);
  }

  public void ReloadPeople() {
    if (Root?.Source is not { } source) return;
    var people = source.GetPeople().OrderBy(x => x.Name).ToList();
    CvPeople.Reload(people, GroupMode.GroupByRecursive, null, true);
  }

  public void RemoveSegments(IList<SegmentM> items) {
    if (Root != null && Root.Source.Any(items.Contains)) {
      TopItem = Root.Source.GetNextOrPreviousItem(items);
      Remove(items.ToArray());
    }
  }
  
  //TODO
  public void Shuffle() {

  }

  //TODO
  public void Sort() {

  }

  private SegmentM[]? _getOneOrSelected(SegmentM? one) {
    if (one == null || Root == null) return null;
    var selected = Root.Source.Where(x => x.IsSelected).ToArray();
    return selected.Contains(one) ? selected : [one];
  }
}