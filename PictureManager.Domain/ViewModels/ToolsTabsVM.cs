using MH.UI.Controls;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Models;
using PictureManager.Domain.ViewModels.Entities;

namespace PictureManager.Domain.ViewModels;

public sealed class ToolsTabsVM : TabControl {
  public PeopleToolsTabVM PeopleTab { get; private set; }
  public PersonVM PersonTab { get; private set; }

  public static RelayCommand<PersonM> OpenPersonTabCommand { get; set; }
  public static RelayCommand OpenPeopleTabCommand { get; set; }

  public ToolsTabsVM() {
    OpenPersonTabCommand = new(OpenPersonTab, Res.IconInformation, "Detail");
    OpenPeopleTabCommand = new(OpenPeopleTab, Res.IconPeopleMultiple, "People");
  }

  private void OpenPeopleTab() {
    PeopleTab ??= new();
    PeopleTab.ReloadFrom();
    Activate(Res.IconPeopleMultiple, "People", PeopleTab);
  }

  private void OpenPersonTab(PersonM person) {
    PersonTab ??= new(Core.S.Person, Core.S.Segment);
    PersonTab.Reload(person);
    Activate(Res.IconPeople, "Person", PersonTab);
  }
}