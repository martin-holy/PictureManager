using MH.UI.Controls;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using PictureManager.Domain.CollectionViews;
using System.Linq;

namespace PictureManager.Domain.Models {
  public sealed class PeopleToolsTabM : CollectionViewPeople {
    private readonly Core _core;

    public PeopleToolsTabM(Core core, PeopleM peopleM) : base(peopleM) {
      _core = core;
    }

    public void ReloadFrom() {
      var md = new MessageDialog(
        "Reload People",
        "From which source do you want to load the people?",
        Res.IconPeople,
        true);

      md.Buttons = new DialogButton[] {
        new("Thumbnails", null, md.SetResult(1), true),
        new("Media Viewer", null, md.SetResult(2)),
        new("All people", null, md.SetResult(3))
      };

      var result = Dialog.Show(md);
      if (result < 1) return;

      var items = result switch {
        1 => PeopleM.GetFromMediaItems(_core.ThumbnailsGridsM.Current?.GetSelectedOrAll().ToArray()),
        2 => PeopleM.GetFromMediaItems(_core.MediaViewerM.MediaItems?.ToArray()),
        3 => PeopleM.GetAll(PeopleM),
        _ => Enumerable.Empty<PersonM>() };

      Reload(items.OrderBy(x => x.Name).ToList(), GroupMode.GroupBy, null);
    }
  }
}
