﻿using MH.UI.Controls;
using MH.UI.Dialogs;
using MH.Utils.Extensions;
using PictureManager.Common.CollectionViews;
using PictureManager.Common.Models;
using PictureManager.Common.Models.MediaItems;
using PictureManager.Common.Services;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Common.ViewModels;

public sealed class PeopleToolsTabVM : CollectionViewPeople {
  private static IEnumerable<PersonM> GetPeople() {
    var md = new MessageDialog(
      "Reload People",
      "From which source do you want to load the people?",
      Res.IconPeople,
      true);

    md.Buttons = [
      new(md.SetResult(1, null, "Thumbnails"), true),
      new(md.SetResult(2, null, "Media Viewer")),
      new(md.SetResult(3, null, "All people"))
    ];

    var result = Dialog.Show(md);
    if (result < 1) return [];

    return result switch {
      1 => Core.VM.MediaItem.Views.Current?.GetSelectedOrAll().GetPeople() ?? [],
      2 => Core.VM.MediaViewer.MediaItems.GetPeople(),
      3 => PersonS.GetAll(),
      _ => []
    };
  }

  public void Reload(PersonM[]? people) {
    var src = (people ?? GetPeople()).EmptyIfNull().OrderBy(x => x.Name).ToList();
    if (src.Count == 0) return;
    Reload(src, GroupMode.ThenByRecursive, null, true);
  }
}