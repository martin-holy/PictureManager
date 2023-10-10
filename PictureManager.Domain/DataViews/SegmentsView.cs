using MH.UI.Controls;
using PictureManager.Domain.CollectionViews;
using PictureManager.Domain.Extensions;
using PictureManager.Domain.Models;
using System.Linq;

namespace PictureManager.Domain.DataViews;

public sealed class SegmentsView {
  public CollectionViewPeople CvPeople { get; }
  public CollectionViewSegments CvSegments { get; }

  public SegmentsView(PeopleM peopleM, SegmentsM segmentsM) {
    CvPeople = new(peopleM);
    CvSegments = new(segmentsM);
  }

  public void Reload(SegmentM[] items) {
    ReloadSegments(items);
    ReloadPeople(items);
  }

  private void ReloadPeople(SegmentM[] items) {
    var source = items.GetPeople().OrderBy(x => x.Name).ToList();
    CvPeople.Reload(source, GroupMode.GroupByRecursive, null, true);
  }

  private void ReloadSegments(SegmentM[] items) {
    var source = items.OrderBy(x => x.MediaItem.FileName).ToList();
    var groupByItems = new[] {
      GroupByItems.GetPeopleInGroupFromSegments(items),
      GroupByItems.GetKeywordsInGroupFromSegments(items)
    };

    CvSegments.Reload(source, GroupMode.ThenByRecursive, groupByItems, true);
  }
}