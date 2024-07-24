using MH.UI.Controls;
using MH.Utils.BaseClasses;
using PictureManager.Common.Features.Person;

namespace PictureManager.Common.Layout;

public sealed class ToolsTabsVM : TabControl {
  public PeopleToolsTabVM? PeopleTab { get; private set; }
  public PersonDetailVM? PersonDetailTab { get; private set; }

  public static RelayCommand<PersonM> OpenPersonTabCommand { get; set; } = null!;
  public static RelayCommand OpenPeopleTabCommand { get; set; } = null!;

  public ToolsTabsVM() : base(new(Dock.Top, Dock.Right, new SlidePanelPinButton()) { JustifyTabSize = true }) {
    OpenPersonTabCommand = new(OpenPersonTab, Res.IconInformation, "Detail");
    OpenPeopleTabCommand = new(() => OpenPeopleTab(null), Res.IconPeopleMultiple, "People");
  }

  public void OpenPeopleTab(PersonM[]? people) {
    PeopleTab ??= new();
    PeopleTab.Reload(people);
    Activate(Res.IconPeopleMultiple, "People", PeopleTab);
  }

  private void OpenPersonTab(PersonM? person) {
    PersonDetailTab ??= new(Core.S.Person, Core.S.Segment);
    PersonDetailTab.Reload(person);
    Activate(Res.IconPeople, "Person", PersonDetailTab);
  }
}