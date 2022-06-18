using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MH.UI.WPF.Controls;
using MH.UI.WPF.Converters;
using MH.UI.WPF.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public sealed class PersonVM : ObservableObject {
    private readonly PeopleM _peopleM;
    private readonly SegmentsM _segmentsM;
    private PersonM _personM;
    private object _scrollToItem;
    private TreeWrapGroup _allSegmentsRoot;

    private readonly HeaderedListItem<object, string> _toolsTabsItem;
    private VirtualizingWrapPanel _topSegmentsPanel;
    private TreeWrapView _allSegmentsPanel;

    public List<SegmentM> AllSegments { get; } = new();
    public ObservableCollection<object> AllSegmentsGrouped { get; } = new();
    public PersonM PersonM { get => _personM; private set { _personM = value; OnPropertyChanged(); } }
    public object ScrollToItem { get => _scrollToItem; set { _scrollToItem = value; OnPropertyChanged(); } }
    public TreeWrapGroup AllSegmentsRoot { get => _allSegmentsRoot; private set { _allSegmentsRoot = value; OnPropertyChanged(); } }
    public RelayCommand<RoutedEventArgs> TopSegmentsLoadedCommand { get; }
    public RelayCommand<RoutedEventArgs> AllSegmentsLoadedCommand { get; }
    public RelayCommand<SizeChangedEventArgs> PanelSizeChangedCommand { get; }
    public RelayCommand<PersonM> SetPersonCommand { get; }
    public RelayCommand<ClickEventArgs> SelectCommand { get; }

    public PersonVM(PeopleM peopleM, SegmentsM segmentsM) {
      _peopleM = peopleM;
      _segmentsM = segmentsM;
      _toolsTabsItem = new(this, "Person");

      TopSegmentsLoadedCommand = new(OnTopSegmentsLoaded);
      AllSegmentsLoadedCommand = new(OnAllSegmentsLoaded);
      PanelSizeChangedCommand = new(PanelSizeChanged);
      SetPersonCommand = new(SetPerson);
      SelectCommand = new(Select);
    }

    private void OnTopSegmentsLoaded(RoutedEventArgs e) {
      _topSegmentsPanel = e.Source as VirtualizingWrapPanel;
      DragDropFactory.SetDrag(_topSegmentsPanel, CanDrag);
      DragDropFactory.SetDrop(_topSegmentsPanel, CanDrop, TopSegmentsDrop);
    }

    private void OnAllSegmentsLoaded(RoutedEventArgs e) {
      _allSegmentsPanel = e.Source as TreeWrapView;
      DragDropFactory.SetDrag(_allSegmentsPanel, CanDrag);
    }

    private object CanDrag(MouseEventArgs e) =>
      (e.OriginalSource as FrameworkElement)?.DataContext as SegmentM;

    private DragDropEffects CanDrop(DragEventArgs e, object source, object data) {
      if (_allSegmentsPanel.Equals(source) && PersonM.TopSegments?.Contains(data as SegmentM) != true)
        return DragDropEffects.Copy;
      if (_topSegmentsPanel.Equals(source) && data != (e.OriginalSource as FrameworkElement)?.DataContext)
        return DragDropEffects.Move;

      return DragDropEffects.None;
    }

    private void TopSegmentsDrop(DragEventArgs e, object source, object data) =>
      _peopleM.ToggleTopSegment(PersonM, data as SegmentM);

    private void SetPerson(PersonM person) {
      _peopleM.DeselectAll();
      if (person != null)
        _peopleM.Select(null, person, false, false);

      PersonM = person;
      ReloadPersonSegments();
      App.Ui.ToolsTabsVM.Activate(_toolsTabsItem, true);
    }

    public void ReloadPersonSegments() {
      if (PersonM == null) return;
      AllSegmentsRoot = PersonM.GetSegments(_segmentsM.DataAdapter.All.Values, AllSegments);
      ScrollToItem = (AllSegmentsRoot?.Items.FirstOrDefault() as TreeWrapGroup)?.Items.FirstOrDefault();
      OnPropertyChanged(nameof(AllSegments)); // this is for the count
    }

    private void Select(ClickEventArgs e) {
      if (e.OriginalSource is Image { DataContext: SegmentM segmentM })
        _segmentsM.Select(AllSegments, segmentM, e.IsCtrlOn, e.IsShiftOn);
    }

    private void PanelSizeChanged(SizeChangedEventArgs e) {
      if (!e.WidthChanged) return;
      _topSegmentsPanel.ReWrap();
      _allSegmentsPanel.ReWrap();
    }
  }
}
