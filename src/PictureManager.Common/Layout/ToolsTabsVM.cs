using MH.UI.Controls;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Common.Features.Person;
using PictureManager.Common.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Common.Layout;

public sealed class ToolsTabsVM : TabControl {
  public PeopleToolsTabVM? PeopleTab { get; private set; }
  public PersonDetailVM? PersonDetailTab { get; private set; }

  public static RelayCommand<PersonM> OpenPersonTabCommand { get; set; } = null!;
  public static AsyncRelayCommand OpenPeopleTabCommand { get; set; } = null!;

  public ToolsTabsVM() : base(new(Dock.Top) { EndSlot = new SlidePanelPinButton(), JustifyTabSize = true }) {
    NoTabsText = "Tools tabs";
    OpenPersonTabCommand = new(_openPersonTab, Res.IconInformation, "Detail");
    OpenPeopleTabCommand = new(_ => OpenPeopleTab(null), Res.IconPeopleMultiple, "People");
  }

  public async Task OpenPeopleTab(PersonM[]? people) {
    PeopleTab ??= new();
    await PeopleTab.Reload(people);
    Activate(Res.IconPeopleMultiple, "People", PeopleTab);
  }

  private void _openPersonTab(PersonM? person) {
    PersonDetailTab ??= new(Core.S.Person, Core.S.Segment);
    PersonDetailTab.Reload(person);
    Activate(Res.IconPeople, "Person", PersonDetailTab);
  }

  public override IEnumerable<ITreeItem> ItemMenuFactory(object item) =>
    MenuFactory.GetMenu(((IListItem)item).Data!)
    .Append(new MenuItem(CloseTabCommand, item));
}