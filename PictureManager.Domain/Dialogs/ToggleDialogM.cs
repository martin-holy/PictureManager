using System;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using PictureManager.Domain.Models;
using System.Collections.Generic;

namespace PictureManager.Domain.Dialogs {
  public sealed class ToggleDialogM : Dialog {
    private const string _msg ="Add or Remove on:";
    private const string _keywordTitle = "Add/Remove Keyword";
    private const string _personTitle = "Add/Remove Person";
    private string _message = _msg;

    public string Message { get => _message; set { _message = value; OnPropertyChanged(); } }
    public ListItem Item { get; }

    public ToggleDialogM(string icon, string title, Tuple<int, int, int> counts, ListItem item) : base(title, icon) {
      Item = item;
      Buttons = GetButtons(counts, this);
    }

    public static void ToggleKeyword(Core core, KeywordM keyword) {
      if (!GetCounts(core, keyword, out var counts)) return;

      var item = new ListItem(Res.IconTagLabel, keyword.FullName);
      var dlg = new ToggleDialogM(Res.IconTagLabel, _keywordTitle, counts, item);
      switch (Show(dlg)) {
        case 1: core.SegmentsM.ToggleKeywordOnSelected(keyword); break;
        case 2: core.PeopleM.ToggleKeywordOnSelected(keyword); break;
        case 3: core.MediaItemsM.SetMetadata(keyword); break;
      }
    }

    public static void TogglePerson(Core core, PersonM person) {
      if (!GetCounts(core, person, out var counts)) return;

      var item = new ListItem(Res.IconPeople, person.Name);
      var dlg = new ToggleDialogM(Res.IconPeople, _personTitle, counts, item);
      switch (Show(dlg)) {
        case 1: core.SegmentsM.SetSelectedAsPerson(person); break;
        case 3: core.MediaItemsM.SetMetadata(person); break;
      }
    }

    private static bool GetCounts(Core core, object item, out Tuple<int, int, int> counts) {
      counts = new(
        core.SegmentsM.Selected.Items.Count,
        item is PersonM ? 0 : core.PeopleM.Selected.Items.Count,
        core.MediaItemsM.IsEditModeOn ? core.MediaItemsM.GetActive().Length : 0);

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
}
