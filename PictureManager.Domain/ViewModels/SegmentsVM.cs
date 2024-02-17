using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Extensions;
using PictureManager.Domain.Database;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models;
using System.Linq;

namespace PictureManager.Domain.ViewModels;

public sealed class SegmentsVM {
  public static IImageSourceConverter<SegmentM> ThumbConverter { get; set; }

  public static RelayCommand SetSelectedAsSamePersonCommand { get; set; }
  public static RelayCommand SetSelectedAsUnknownCommand { get; set; }

  public SegmentsVM(SegmentsM m, SegmentsDA da) {
    SetSelectedAsSamePersonCommand = new(
      () => m.SetSelectedAsSamePerson(m.Selected.Items.ToArray()), Res.IconEquals, "Set selected as same person");
    SetSelectedAsUnknownCommand = new(
      () => SetAsUnknown(m.Selected.Items.ToArray(), da),
      () => m.Selected.Items.Count > 0, Res.IconUnknownSegment, "Set selected as Unknown");
  }

  private static void SetAsUnknown(SegmentM[] segments, SegmentsDA da) {
    var msg = "Do you want to set {0} segment{1} as unknown?".Plural(segments.Length);
    if (Dialog.Show(new MessageDialog("Set as unknown", msg, Res.IconQuestion, true)) != 1) return;
    da.ChangePerson(null, segments, segments.GetPeople().ToArray());
  }
}