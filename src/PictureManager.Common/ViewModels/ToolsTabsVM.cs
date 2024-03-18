using MH.UI.Controls;
using MH.Utils.BaseClasses;
using PictureManager.Common.Models;
using PictureManager.Common.ViewModels.Entities;

namespace PictureManager.Common.ViewModels;

public sealed class ToolsTabsVM : TabControl {
  public PeopleToolsTabVM PeopleTab { get; private set; }
  public PersonDetailVM PersonDetailTab { get; private set; }

  public static RelayCommand<PersonM> OpenPersonTabCommand { get; set; }
  public static RelayCommand OpenPeopleTabCommand { get; set; }

  public ToolsTabsVM() {
    OpenPersonTabCommand = new(OpenPersonTab, Res.IconInformation, "Detail");
    OpenPeopleTabCommand = new(() => OpenPeopleTab(null), Res.IconPeopleMultiple, "People");
  }

  public void OpenPeopleTab(PersonM[] people) {
    PeopleTab ??= new();
    PeopleTab.Reload(people);
    Activate(Res.IconPeopleMultiple, "People", PeopleTab);
  }

  private void OpenPersonTab(PersonM person) {
    PersonDetailTab ??= new(Core.S.Person, Core.S.Segment);
    PersonDetailTab.Reload(person);
    Activate(Res.IconPeople, "Person", PersonDetailTab);
  }
}