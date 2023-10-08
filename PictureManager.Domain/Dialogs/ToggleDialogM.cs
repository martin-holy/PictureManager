using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Dialogs; 

public sealed class ToggleDialogM : Dialog {
  private static ToggleDialogM _instance;
  private const string _message ="Add or Remove on:";
  private const string _keywordTitle = "Add/Remove Keyword";
  private const string _personTitle = "Add/Remove Person";

  public string Message => _message;
  public ListItem Item { get; set; }

  public ToggleDialogM(string icon, string title) : base(title, icon) { }

  private static ToggleDialogM GetInstance(string icon, string title, Tuple<int, int, int> counts, ListItem item) {
    _instance ??= new(icon, title);
    _instance.Icon = icon;
    _instance.Title = title;
    _instance.Item = item;
    _instance.Buttons = GetButtons(counts, _instance);

    return _instance;
  }

  public static void ToggleKeyword(KeywordM keyword) {
    if (keyword == null || !GetCounts(keyword, out var counts)) return;

    var dlg = GetInstance(Res.IconTagLabel, _keywordTitle, counts, keyword);
    switch (Show(dlg)) {
      case 1: Core.Db.Segments.ToggleKeyword(Core.SegmentsM.Selected.Items.ToArray(), keyword); break;
      case 2: Core.Db.People.ToggleKeyword(Core.PeopleM.Selected.Items.ToArray(), keyword); break;
      case 3: Core.MediaItemsM.SetMetadata(keyword); break;
    }
  }

  public static void TogglePerson(PersonM person) {
    if (person == null || !GetCounts(person, out var counts)) return;

    var dlg = GetInstance(Res.IconPeople, _personTitle, counts, person);
    switch (Show(dlg)) {
      case 1: Core.SegmentsM.SetSelectedAsPerson(person); break;
      case 3: Core.MediaItemsM.SetMetadata(person); break;
    }
  }

  private static bool GetCounts(object item, out Tuple<int, int, int> counts) {
    counts = new(
      Core.SegmentsM.Selected.Items.Count,
      item is PersonM ? 0 : Core.PeopleM.Selected.Items.Count,
      Core.MediaItemsM.IsEditModeOn ? Core.MediaItemsM.GetActive().Length : 0);

    return counts.Item1 != 0 || counts.Item2 != 0 || counts.Item3 != 0;
  }

  private static DialogButton[] GetButtons(Tuple<int, int, int> counts, Dialog dlg) {
    var (sCount, pCount, miCount) = counts;
    var buttons = new List<DialogButton>();

    if (sCount > 0)
      buttons.Add(new(GetTitle("Segment", "Segments", sCount), Res.IconSegment, dlg.SetResult(1)));
    if (pCount > 0)
      buttons.Add(new(GetTitle("Person", "People", pCount), Res.IconPeople, dlg.SetResult(2)));
    if (miCount > 0)
      buttons.Add(new(GetTitle("Media Item", "Media Items", miCount), Res.IconImage, dlg.SetResult(3)));

    return buttons.ToArray();
  }

  private static string GetTitle(string one, string many, int count) =>
    count > 1 ? $"{many} ({count})" : one;
}