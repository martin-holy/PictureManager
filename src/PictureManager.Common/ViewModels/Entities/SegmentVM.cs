﻿using MH.UI.Controls;
using MH.UI.Dialogs;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Common.Interfaces;
using PictureManager.Common.Models;
using PictureManager.Common.Repositories;
using PictureManager.Common.Services;
using System.Linq;

namespace PictureManager.Common.ViewModels.Entities;

public sealed class SegmentVM : ObservableObject {
  private readonly CoreVM _coreVM;
  private readonly SegmentS _s;
  private readonly SegmentR _r;

  public static int SegmentSize { get; set; } = 100;
  public static int SegmentUiSize { get; set; }
  public static int SegmentUiFullWidth { get; set; }
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
    if (Dialog.Show(new MessageDialog("Set as unknown", msg, MH.UI.Res.IconQuestion, true)) != 1) return;
    r.ChangePerson(null, segments, segments.GetPeople().ToArray());
  }

  public static void SetSegmentUiSize(double scale) {
    var size = (int)(SegmentSize / scale);
    SegmentUiSize = size;
    SegmentUiFullWidth = size + 6; // + border, margin
  }
}