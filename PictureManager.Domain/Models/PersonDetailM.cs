using MH.Utils.BaseClasses;
using MH.Utils.EventsArgs;
using static MH.Utils.DragDropHelper;
using System.Collections.Generic;

namespace PictureManager.Domain.Models {
  public sealed class PersonDetailM : ObservableObject {
    private readonly PeopleM _peopleM;
    private readonly SegmentsM _segmentsM;
    private PersonM _personM;
    private bool _scrollToTop;
    private TreeWrapGroup _allSegmentsRoot;

    public HeaderedListItem<object, string> ToolsTabsItem;
    public List<SegmentM> AllSegments { get; } = new();
    public PersonM PersonM { get => _personM; set { _personM = value; ReloadPersonSegments(); OnPropertyChanged(); } }
    public bool ScrollToTop { get => _scrollToTop; set { _scrollToTop = value; OnPropertyChanged(); } }
    public TreeWrapGroup AllSegmentsRoot { get => _allSegmentsRoot; private set { _allSegmentsRoot = value; OnPropertyChanged(); } }
    public CanDropFunc CanDropFunc { get; }
    public DoDropAction TopSegmentsDropAction { get; }
    public RelayCommand<PersonM> SetPersonCommand { get; }
    public RelayCommand<MouseButtonEventArgs> SelectCommand { get; }

    public PersonDetailM(PeopleM peopleM, SegmentsM segmentsM) {
      _peopleM = peopleM;
      _segmentsM = segmentsM;
      ToolsTabsItem = new(this, "Person");

      CanDropFunc = CanDrop;
      TopSegmentsDropAction = TopSegmentsDrop;

      SetPersonCommand = new(SetPerson);
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

    private void SetPerson(PersonM person) {
      PersonM = person;
      Core.Instance.ToolsTabsM.Activate(ToolsTabsItem, true);
    }

    public void ReloadPersonSegments() {
      ScrollToTop = true;
      AllSegmentsRoot = PersonM.GetSegments(_segmentsM.DataAdapter.All, AllSegments);
      OnPropertyChanged(nameof(AllSegments)); // this is for the count
    }

    private void Select(MouseButtonEventArgs e) {
      if (e.IsSourceDesired && e.DataContext is SegmentM segmentM)
        _segmentsM.Select(AllSegments, segmentM, e.IsCtrlOn, e.IsShiftOn);
    }
  }
}
