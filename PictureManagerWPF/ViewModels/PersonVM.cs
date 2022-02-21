using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Converters;
using MH.Utils.BaseClasses;
using PictureManager.CustomControls;
using PictureManager.Domain.Models;
using PictureManager.Utils;
using ObservableObject = MH.Utils.BaseClasses.ObservableObject;

namespace PictureManager.ViewModels {
  public sealed class PersonVM : ObservableObject {
    private readonly PeopleM _peopleM;
    private readonly SegmentsM _segmentsM;
    private readonly int _segmentGridWidth = 100 + 6; //border, margin, padding, ... //TODO find the real value
    private PersonM _personM;
    private readonly HeaderedListItem<object, string> _toolsTabsItem;

    public List<SegmentM> AllSegments { get; } = new();
    public VirtualizingWrapPanel TopSegmentsPanel { get; }
    public VirtualizingWrapPanel AllSegmentsPanel { get; }
    public RelayCommand<PersonM> SetPersonCommand { get; }
    public RelayCommand<ClickEventArgs> SelectCommand { get; }
    public PersonM PersonM { get => _personM; set { _personM = value; OnPropertyChanged(); } }

    public PersonVM(PeopleM peopleM, SegmentsM segmentsM) {
      _peopleM = peopleM;
      _segmentsM = segmentsM;
      _toolsTabsItem = new(this, "Person");

      TopSegmentsPanel = new();
      AllSegmentsPanel = new();

      TopSegmentsPanel.Style = (Style)Application.Current.FindResource("Views.PersonV.TopSegmentsPanel");
      AllSegmentsPanel.Style = (Style)Application.Current.FindResource("Views.PersonV.AllSegmentsPanel");

      DragDropFactory.SetDrag(TopSegmentsPanel, CanDrag);
      DragDropFactory.SetDrop(TopSegmentsPanel, CanDrop, TopSegmentsDrop);
      DragDropFactory.SetDrag(AllSegmentsPanel, CanDrag);

      SetPersonCommand = new(SetPerson);
      SelectCommand = new(Select);
    }

    private object CanDrag(MouseEventArgs e) =>
      (e.OriginalSource as FrameworkElement)?.DataContext as SegmentM;

    private DragDropEffects CanDrop(DragEventArgs e, object source, object data) {
      if (AllSegmentsPanel.Equals(source) && PersonM.Segments?.Contains(data as SegmentM) != true)
        return DragDropEffects.Copy;
      if (TopSegmentsPanel.Equals(source) && data != (e.OriginalSource as FrameworkElement)?.DataContext)
        return DragDropEffects.Move;

      return DragDropEffects.None;
    }

    private void TopSegmentsDrop(DragEventArgs e, object source, object data) {
      if (data is SegmentM segmentM)
        ToggleSegment(segmentM);
    }

    public void ToggleSegment(SegmentM segment) {
      _peopleM.ToggleSegment(PersonM, segment);
      ReloadTopSegmentsPanel();
    }

    private void SetPerson(PersonM person) {
      PersonM = person;
      ReloadPersonSegments();
      App.Ui.ToolsTabsVM.Activate(_toolsTabsItem, true);
    }

    public void ReloadPersonSegments() {
      AllSegments.Clear();
      ReloadTopSegmentsPanel();
      ReloadAllSegmentsPanel();
      OnPropertyChanged(nameof(AllSegments));
    }

    private void ReloadTopSegmentsPanel() {
      TopSegmentsPanel.ClearRows();
      if (PersonM?.Segments == null) return;

      // TODO try to do it without UpdateLayout
      TopSegmentsPanel.UpdateLayout();
      TopSegmentsPanel.UpdateMaxRowWidth();

      foreach (var segment in PersonM.Segments)
        TopSegmentsPanel.AddItem(segment, _segmentGridWidth);

      TopSegmentsPanel.ScrollToTop();
    }

    private void ReloadAllSegmentsPanel() {
      AllSegmentsPanel.ClearRows();
      if (PersonM == null) return;

      // TODO try to do it without UpdateLayout
      AllSegmentsPanel.UpdateLayout();
      AllSegmentsPanel.UpdateMaxRowWidth();

      foreach (var group in _segmentsM.All
        .Where(x => x.PersonId == PersonM.Id)
        .GroupBy(x => x.Keywords == null
          ? string.Empty
          : string.Join(", ", KeywordsM.GetAllKeywords(x.Keywords).Select(k => k.Name)))
        .OrderBy(x => x.Key)) {

        // add group
        if (!string.IsNullOrEmpty(group.Key))
          AllSegmentsPanel.AddGroup("IconTag", group.Key);

        // add segments
        foreach (var segment in group.OrderBy(x => x.MediaItem.FileName)) {
          AllSegmentsPanel.AddItem(segment, _segmentGridWidth);
          AllSegments.Add(segment);
        }
      }

      AllSegmentsPanel.ScrollToTop();
    }

    private void Select(ClickEventArgs e) {
      if (e.OriginalSource is Image { DataContext: SegmentM segmentM })
        _segmentsM.Select(AllSegments, segmentM, e.IsCtrlOn, e.IsShiftOn);
    }
  }
}
