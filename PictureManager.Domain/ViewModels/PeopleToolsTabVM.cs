using MH.UI.Controls;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Extensions;
using PictureManager.Domain.CollectionViews;
using PictureManager.Domain.Models;
using PictureManager.Domain.Models.MediaItems;
using PictureManager.Domain.Services;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.ViewModels;

public sealed class PeopleToolsTabVM : CollectionViewPeople {
  private static IEnumerable<PersonM> GetPeople() {
    var md = new MessageDialog(
      "Reload People",
      "From which source do you want to load the people?",
      Res.IconPeople,
      true);

    md.Buttons = new DialogButton[] {
      new(md.SetResult(1, null, "Thumbnails"), true),
      new(md.SetResult(2, null, "Media Viewer")),
      new(md.SetResult(3, null, "All people"))
    };

    var result = Dialog.Show(md);
    if (result < 1) return Enumerable.Empty<PersonM>();

    return result switch {
      1 => Core.VM.MediaItemsViews.Current?.GetSelectedOrAll().GetPeople(),
      2 => Core.VM.MediaViewer.MediaItems.GetPeople(),
      3 => PersonS.GetAll(),
      _ => Enumerable.Empty<PersonM>()
    };
  }

  public void Reload(PersonM[] people) =>
    Reload((people ?? GetPeople()).EmptyIfNull().OrderBy(x => x.Name).ToList(), GroupMode.ThenByRecursive, null, true);
}