﻿using MH.UI.Controls;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using PictureManager.Domain.CollectionViews;
using System.Linq;

namespace PictureManager.Domain.Models; 

public sealed class PeopleToolsTabM : CollectionViewPeople {
  public PeopleToolsTabM(PeopleM peopleM) : base(peopleM) { }

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
      1 => PeopleM.GetFromMediaItems(Core.MediaItemsViews.Current?.GetSelectedOrAll()),
      2 => PeopleM.GetFromMediaItems(Core.MediaViewerM.MediaItems),
      3 => PeopleM.GetAll(),
      _ => Enumerable.Empty<PersonM>() };

    Reload(items.OrderBy(x => x.Name).ToList(), GroupMode.GroupBy, null, true);
  }
}