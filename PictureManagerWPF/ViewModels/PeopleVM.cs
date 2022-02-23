using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Converters;
using MH.Utils.BaseClasses;
using PictureManager.CustomControls;
using PictureManager.Domain.Models;
using ObservableObject = MH.Utils.BaseClasses.ObservableObject;

namespace PictureManager.ViewModels {
  public sealed class PeopleVM : ObservableObject {
    private readonly int _segmentGridWidth = 100 + 6; //border, margin, padding, ... //TODO find the real value

    public PeopleM PeopleM { get; set; }
    public VirtualizingWrapPanel Panel { get; }
    public HeaderedListItem<object, string> MainTabsItem { get; }
    public RelayCommand<ClickEventArgs> SelectCommand { get; }

    public PeopleVM(PeopleM peopleM) {
      PeopleM = peopleM;

      MainTabsItem = new(this, "People");

      Panel = new() {
        Style = (Style)Application.Current.FindResource("Views.PeopleV.Panel")
      };

      Panel.SizeChanged += (o, e) => {
        if (e.WidthChanged && !App.Ui.MainWindowVM.IsFullScreenIsChanging)
          Reload();
      };

      SelectCommand = new(Select);

      // TODO do it just for loaded
      foreach (var person in App.Core.PeopleM.All)
        person.UpdateDisplayKeywords();
    }

    private void Select(ClickEventArgs e) {
      if (e.OriginalSource is Image { DataContext: SegmentM segmentM })
        PeopleM.Select(null, segmentM.Person, e.IsCtrlOn, e.IsShiftOn);

      if (e.OriginalSource is FrameworkElement { DataContext: PersonM personM })
        PeopleM.Select(null, personM, e.IsCtrlOn, e.IsShiftOn);
    }

    public void Reload() {
      var rowIndex = Panel.GetTopRowIndex();
      var itemToScrollTo = Panel.GetFirstItemFromRow(rowIndex);
      Panel.ClearRows();
      Panel.UpdateLayout();
      Panel.UpdateMaxRowWidth();

      // add people in groups
      foreach (var group in PeopleM.Items.OfType<CategoryGroupM>().Where(x => !x.IsHidden)) {
        Panel.AddGroup("IconPeople", group.Name);
        AddPeople(group.Name, group.Items.Cast<PersonM>());
      }

      // add people without group
      var peopleWithoutGroup = PeopleM.Items.OfType<PersonM>().ToArray();
      if (peopleWithoutGroup.Any()) {
        Panel.AddGroup("IconPeople", string.Empty);
        AddPeople(string.Empty, peopleWithoutGroup);
      }

      if (rowIndex > 0)
        Panel.ScrollTo(itemToScrollTo);
    }

    private void AddPeople(string groupTitle, IEnumerable<PersonM> people) {
      // group people by keywords
      foreach (var group in people
                 .GroupBy(p => p.DisplayKeywords == null
                   ? string.Empty
                   : string.Join(", ", p.DisplayKeywords.Select(dk => dk.FullName)))
                 .OrderBy(g => g.Key)) {

        // add group
        if (!group.Key.Equals(string.Empty)) {
          var groupItems = new List<VirtualizingWrapPanelGroupItem> { new() { Icon = "IconTag", Title = group.Key } };
          if (!string.IsNullOrEmpty(groupTitle))
            groupItems.Insert(0, new() { Icon = "IconPeople", Title = groupTitle });
          Panel.AddGroup(groupItems.ToArray());
        }

        // add people
        foreach (var person in group.OrderBy(p => p.Name))
          Panel.AddItem(person, _segmentGridWidth);
      }
    }
  }
}
