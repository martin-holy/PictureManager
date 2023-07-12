using MH.UI.Controls;
using MH.Utils.BaseClasses;
using PictureManager.Domain.CollectionViews;
using PictureManager.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using MH.Utils.EventsArgs;
using static MH.Utils.DragDropHelper;

namespace PictureManager.Domain.DataViews {
  public sealed class PersonDetail : ObservableObject {
    private readonly PeopleM _peopleM;
    private readonly SegmentsM _segmentsM;
    private PersonM _personM;

    public CollectionViewSegments AllSegments { get; }
    public PersonM PersonM { get => _personM; set { _personM = value; OnPropertyChanged(); } }
    public CanDropFunc CanDropFunc { get; }
    public DoDropAction TopSegmentsDropAction { get; }
    public RelayCommand<MouseButtonEventArgs> SelectCommand { get; }

    public PersonDetail(PeopleM peopleM, SegmentsM segmentsM) {
      _peopleM = peopleM;
      _segmentsM = segmentsM;
      AllSegments = new(segmentsM);

      CanDropFunc = CanDrop;
      TopSegmentsDropAction = TopSegmentsDrop;
      SelectCommand = new(Select);
    }

    private MH.Utils.DragDropEffects CanDrop(object target, object data, bool haveSameOrigin) {
      if (!haveSameOrigin && PersonM.TopSegments?.Contains(data as SegmentM) != true)
        return MH.Utils.DragDropEffects.Copy;
      if (haveSameOrigin && data != target)
        return MH.Utils.DragDropEffects.Move;

      return MH.Utils.DragDropEffects.None;
    }

    private void TopSegmentsDrop(object data, bool haveSameOrigin) =>
      _peopleM.ToggleTopSegment(PersonM, data as SegmentM);

    public void Reload(PersonM person) {
      PersonM = person;

      if (PersonM == null) {
        AllSegments.Root.Items.Clear();
        return;
      }

      ReloadAllSegments(_segmentsM.DataAdapter.All
        .Where(x => ReferenceEquals(x.Person, PersonM))
        .ToList());
    }

    private void ReloadAllSegments(IReadOnlyCollection<SegmentM> items) {
      var source = items
        .OrderBy(x => x.MediaItem.FileName)
        .ToList();
      var groupByItems = new[] {
        GroupByItems.GetKeywordsInGroupFromSegments(items)
      };

      AllSegments.Reload(source, GroupMode.ThenByRecursive, groupByItems, true);
    }

    public void ReGroupIfContains(IEnumerable<SegmentM> segments, bool remove) {
      if (PersonM == null) return;
      var items = segments.Where(x => ReferenceEquals(PersonM, x.Person)).ToArray();
      if (items.Length == 0) return;
      AllSegments.ReGroupItems(items, remove);
    }

    private void Select(MouseButtonEventArgs e) {
      if (e.IsSourceDesired && e.DataContext is SegmentM segmentM)
        _segmentsM.Select(AllSegments.Root.Source, segmentM, e.IsCtrlOn, e.IsShiftOn);
    }
  }
}
