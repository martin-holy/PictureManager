using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Extensions;
using PictureManager.Domain.Models;
using PictureManager.Domain.Models.MediaItems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Dialogs;

public sealed class ToggleDialogM : Dialog {
  private static ToggleDialogM _inst;

  public string Message { get; private set; }
  public ListItem Item { get; private set; }
  public SegmentM[] Segments { get; private set; }
  public PersonM[] People { get; private set; }
  public MediaItemM[] MediaItems { get; private set; }
  public VideoItemM[] VideoItems { get; private set; }

  private ToggleDialogM(string icon, string title) : base(title, icon) { }

  private static ToggleDialogM GetInstance(string icon, string title, string message, ListItem item) {
    if (item == null) return null;

    _inst ??= new(icon, title);
    
    if (!_inst.GetItems(item)) return null;

    _inst.Icon = icon;
    _inst.Title = title;
    _inst.Message = message;
    _inst.Item = item;
    _inst.Buttons = _inst.GetButtons().ToArray();

    return _inst;
  }

  private bool GetItems(object item) {
    var person = item as PersonM;
    Segments = Core.S.Segment.Selected.Items.ToArray();
    People = person == null ? Core.S.Person.Selected.Items.ToArray() : Array.Empty<PersonM>();
    MediaItems = Core.VM.GetActive<MediaItemM>();
    VideoItems = Core.VideoDetail.CurrentVideoItems.Selected.Items.ToArray();

    if (person != null) {
      MediaItems = MediaItems.Where(mi => mi.Segments?.GetPeople().Contains(person) != true).ToArray();
      VideoItems = VideoItems.Where(mi => mi.Segments?.GetPeople().Contains(person) != true).ToArray();
    }

    return Segments.Length + People.Length + MediaItems.Length + VideoItems.Length > 0;
  }

  private IEnumerable<DialogButton> GetButtons() {
    if (Segments.Length > 0)
      yield return new(SetResult(1, Res.IconSegment, "{0} Segment{1}".Plural(Segments.Length)));
    if (People.Length > 0)
      yield return new(SetResult(2, Res.IconPeople, "{0} Person{1}".Plural(People.Length)));
    if (MediaItems.Length > 0)
      yield return new(SetResult(3, Res.IconImage, "{0} Media Item{1}".Plural(MediaItems.Length)));
    if (VideoItems.Length > 0)
      yield return new(SetResult(4, Res.IconMovieClapper, "{0} Video Item{1}".Plural(VideoItems.Length)));
  }

  public static void Clear() {
    if (_inst == null) return;
    _inst.Segments = null;
    _inst.People = null;
    _inst.MediaItems = null;
    _inst.VideoItems = null;
  }

  public static void ToggleKeyword(KeywordM keyword) {
    if (GetInstance(Res.IconTagLabel, "Add/Remove Keyword", "Add or Remove on:", keyword) is not { } dlg) return;

    switch (Show(dlg)) {
      case 1: Core.R.Segment.ToggleKeyword(dlg.Segments, keyword); break;
      case 2: Core.R.Person.ToggleKeyword(dlg.People, keyword); break;
      case 3: Core.R.MediaItem.ToggleKeyword(dlg.MediaItems, keyword); break;
      case 4: Core.R.MediaItem.ToggleKeyword(dlg.VideoItems.Cast<MediaItemM>().ToArray(), keyword); break;
    }

    Clear();
  }

  public static void TogglePerson(PersonM person) {
    if (GetInstance(Res.IconPeople, "Add/Remove Person", "Add or Remove on:", person) is not { } dlg) return;

    switch (Show(dlg)) {
      case 1: Core.S.Segment.SetSelectedAsPerson(dlg.Segments, person); break;
      case 3: Core.R.MediaItem.TogglePerson(dlg.MediaItems, person); break;
      case 4: Core.R.MediaItem.TogglePerson(dlg.VideoItems.Cast<MediaItemM>().ToArray(), person); break;
    }

    Clear();
  }

  public static void SetGeoName(GeoNameM geoName) {
    if (GetInstance(Res.IconPeople, "Set GeoName", "Set GeoName on:", geoName) is not { } dlg) return;

    switch (Show(dlg)) {
      case 3: Core.R.MediaItem.SetGeoName(dlg.MediaItems, geoName); break;
      case 4: Core.R.MediaItem.SetGeoName(dlg.VideoItems.Cast<MediaItemM>().ToArray(), geoName); break;
    }

    Clear();
  }

  public static void SetRating(RatingTreeM rating) {
    if (GetInstance(Res.IconStar, "Set Rating", "Set Rating on:", rating) is not { } dlg) return;

    switch (Show(dlg)) {
      case 3: Core.R.MediaItem.SetRating(dlg.MediaItems, rating.Rating); break;
      case 4: Core.R.MediaItem.SetRating(dlg.VideoItems.Cast<MediaItemM>().ToArray(), rating.Rating); break;
    }

    Clear();
  }
}