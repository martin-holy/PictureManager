using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Extensions;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models;
using PictureManager.Domain.Repositories;
using PictureManager.Domain.Services;
using System.Linq;

namespace PictureManager.Domain.ViewModels.Entities;

public sealed class SegmentVM : ObservableObject {
  private readonly CoreVM _coreVM;
  private readonly SegmentS _s;
  private readonly SegmentR _r;
  public static IImageSourceConverter<SegmentM> ThumbConverter { get; set; }

  public SegmentRectVM Rect { get; } = new();

  public static RelayCommand<KeywordM> LoadByKeywordCommand { get; set; }
  public static RelayCommand<PersonM> LoadByPersonCommand { get; set; }
  public static RelayCommand SetSelectedAsSamePersonCommand { get; set; }
  public static RelayCommand SetSelectedAsUnknownCommand { get; set; }

  public SegmentVM(CoreVM coreVM, SegmentS s, SegmentR r) {
    _coreVM = coreVM;
    _s = s;
    _r = r;
    LoadByKeywordCommand = new(LoadBy, Res.IconSegment, "Load Segments");
    LoadByPersonCommand = new(LoadBy, Res.IconSegment, "Load Segments");
    SetSelectedAsSamePersonCommand = new(SetSelectedAsSamePerson, Res.IconEquals, "Set selected as same person");
    SetSelectedAsUnknownCommand = new(SetSelectedAsUnknown, CanSetSelectedAsUnknown, Res.IconUnknownSegment, "Set selected as Unknown");
  }

  private void LoadBy(KeywordM k) =>
    _coreVM.OpenSegmentsMatching(_r.GetBy(k, Keyboard.IsShiftOn()).ToArray());

  private void LoadBy(PersonM p) =>
    _coreVM.OpenSegmentsMatching(_r.GetBy(p).ToArray());

  private void SetSelectedAsSamePerson() =>
    _s.SetSelectedAsSamePerson(_s.Selected.Items.ToArray());

  private void SetSelectedAsUnknown() =>
    SetAsUnknown(_s.Selected.Items.ToArray(), _r);

  private bool CanSetSelectedAsUnknown() =>
    _s.Selected.Items.Count > 0;

  private static void SetAsUnknown(SegmentM[] segments, SegmentR r) {
    var msg = "Do you want to set {0} segment{1} as unknown?".Plural(segments.Length);
    if (Dialog.Show(new MessageDialog("Set as unknown", msg, Res.IconQuestion, true)) != 1) return;
    r.ChangePerson(null, segments, segments.GetPeople().ToArray());
  }
}