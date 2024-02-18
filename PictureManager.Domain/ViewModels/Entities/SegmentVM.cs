using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Extensions;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models;
using PictureManager.Domain.Repositories;
using PictureManager.Domain.Services;
using System.Linq;

namespace PictureManager.Domain.ViewModels.Entities;

public sealed class SegmentVM {
  public static IImageSourceConverter<SegmentM> ThumbConverter { get; set; }

  public SegmentRectVM Rect { get; } = new();

  public static RelayCommand SetSelectedAsSamePersonCommand { get; set; }
  public static RelayCommand SetSelectedAsUnknownCommand { get; set; }

  public SegmentVM(SegmentS s, SegmentR r) {
    SetSelectedAsSamePersonCommand = new(
      () => s.SetSelectedAsSamePerson(s.Selected.Items.ToArray()), Res.IconEquals, "Set selected as same person");
    SetSelectedAsUnknownCommand = new(
      () => SetAsUnknown(s.Selected.Items.ToArray(), r),
      () => s.Selected.Items.Count > 0, Res.IconUnknownSegment, "Set selected as Unknown");
  }

  private static void SetAsUnknown(SegmentM[] segments, SegmentR da) {
    var msg = "Do you want to set {0} segment{1} as unknown?".Plural(segments.Length);
    if (Dialog.Show(new MessageDialog("Set as unknown", msg, Res.IconQuestion, true)) != 1) return;
    da.ChangePerson(null, segments, segments.GetPeople().ToArray());
  }
}