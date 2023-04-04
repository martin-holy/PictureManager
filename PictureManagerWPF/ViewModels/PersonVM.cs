using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MH.UI.WPF.Controls;
using MH.UI.WPF.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.EventsArgs;
using PictureManager.Domain.Models;
using static MH.Utils.DragDropHelper;

namespace PictureManager.ViewModels {
  public sealed class PersonVM : ObservableObject {
    private readonly PeopleM _peopleM;
    private readonly SegmentsM _segmentsM;
    private PersonM _personM;
    private object _scrollToItem;
    private TreeWrapGroup _allSegmentsRoot;
    private bool _reWrapTopItems;
    private bool _reWrapAllItems;

    private readonly HeaderedListItem<object, string> _toolsTabsItem;
    private VirtualizingWrapPanel _topSegmentsPanel;
    private TreeWrapView _allSegmentsPanel;

    public List<SegmentM> AllSegments { get; } = new();
    public ObservableCollection<object> AllSegmentsGrouped { get; } = new();
    public PersonM PersonM { get => _personM; private set { _personM = value; OnPropertyChanged(); } }
    public object ScrollToItem { get => _scrollToItem; set { _scrollToItem = value; OnPropertyChanged(); } }
    public TreeWrapGroup AllSegmentsRoot { get => _allSegmentsRoot; private set { _allSegmentsRoot = value; OnPropertyChanged(); } }
    public bool ReWrapTopItems { get => _reWrapTopItems; set { _reWrapTopItems = value; OnPropertyChanged(); } }
    public bool ReWrapAllItems { get => _reWrapAllItems; set { _reWrapAllItems = value; OnPropertyChanged(); } }
    public CanDropFunc CanDropFunc { get; }
    public DoDropAction TopSegmentsDropAction { get; }
    public RelayCommand<PersonM> SetPersonCommand { get; }
    public RelayCommand<ClickEventArgs> SelectCommand { get; }
    public RelayCommand<object> PanelTopWidthChangedCommand { get; }
    public RelayCommand<object> PanelAllWidthChangedCommand { get; }

    public PersonVM(PeopleM peopleM, SegmentsM segmentsM) {
      _peopleM = peopleM;
      _segmentsM = segmentsM;
      _toolsTabsItem = new(this, "Person");

      CanDropFunc = CanDrop;
      TopSegmentsDropAction = TopSegmentsDrop;

      SetPersonCommand = new(SetPerson);
      SelectCommand = new(Select);
      PanelTopWidthChangedCommand = new(() => ReWrapTopItems = true);
      PanelAllWidthChangedCommand = new(() => ReWrapAllItems = true);
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
      _peopleM.DeselectAll();
      if (person != null)
        _peopleM.Select(null, person, false, false);

      PersonM = person;
      ReloadPersonSegments();
      App.Core.ToolsTabsM.Activate(_toolsTabsItem, true);
    }

    public void ReloadPersonSegments() {
      if (PersonM == null) return;
      AllSegmentsRoot = PersonM.GetSegments(_segmentsM.DataAdapter.All.Values, AllSegments);
      ScrollToItem = (AllSegmentsRoot?.Items.FirstOrDefault() as TreeWrapGroup)?.Items.FirstOrDefault();
      OnPropertyChanged(nameof(AllSegments)); // this is for the count
    }

    private void Select(ClickEventArgs e) {
      if (e.IsSourceDesired && e.DataContext is SegmentM segmentM)
        _segmentsM.Select(AllSegments, segmentM, e.IsCtrlOn, e.IsShiftOn);
    }
  }
}
