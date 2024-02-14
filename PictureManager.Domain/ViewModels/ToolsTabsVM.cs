using MH.UI.Controls;
using MH.Utils.BaseClasses;
using PictureManager.Domain.DataViews;
using PictureManager.Domain.Models;

namespace PictureManager.Domain.ViewModels;

public sealed class ToolsTabsVM : TabControl {
  public PeopleToolsTabVM PeopleTab { get; private set; }
  public PersonDetail PersonDetailTab { get; private set; }

  public static RelayCommand<PersonM> OpenPersonDetailTabCommand { get; set; }
  public static RelayCommand OpenPeopleTabCommand { get; set; }

  public ToolsTabsVM() {
    OpenPersonDetailTabCommand = new(OpenPersonDetailTab, Res.IconInformation, "Detail");
    OpenPeopleTabCommand = new(OpenPeopleTab, Res.IconPeopleMultiple, "People");
  }

  private void OpenPeopleTab() {
    PeopleTab ??= new();
    PeopleTab.ReloadFrom();
    Activate(Res.IconPeopleMultiple, "People", PeopleTab);
  }

  private void OpenPersonDetailTab(PersonM person) {
    PersonDetailTab ??= new(Core.PeopleM, Core.SegmentsM);
    PersonDetailTab.Reload(person);
    Activate(Res.IconPeople, "Person", PersonDetailTab);
  }
}