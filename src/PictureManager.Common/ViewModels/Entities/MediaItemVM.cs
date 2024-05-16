﻿using MH.UI.Controls;
using MH.UI.Dialogs;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Common.Interfaces;
using PictureManager.Common.Models;
using PictureManager.Common.Models.MediaItems;
using PictureManager.Common.Services;
using PictureManager.Common.Utils;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Common.ViewModels.Entities;

public sealed class MediaItemVM : ObservableObject {
  private readonly CoreVM _coreVM;
  private readonly MediaItemS _s;
  private MediaItemM _current;
  private int _itemsCount;

  public static IImageSourceConverter<MediaItemM> ThumbConverter { get; set; }

  public MediaItemM Current { get => _current; set { _current = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentGeoName)); } }
  public GeoNameM CurrentGeoName => Current?.GeoLocation?.GeoName;
  public MediaItemsViewsVM Views { get; } = new();
  public int ItemsCount { get => _itemsCount; set { _itemsCount = value; OnPropertyChanged(); } }

  public static RelayCommand CommentCommand { get; set; }
  public static RelayCommand DeleteCommand { get; set; }
  public static RelayCommand<GeoNameM> LoadByGeoNameCommand { get; set; }
  public static RelayCommand<KeywordM> LoadByKeywordCommand { get; set; }
  public static RelayCommand<PersonM> LoadByPersonCommand { get; set; }
  public static RelayCommand LoadByPeopleOrSegmentsCommand { get; set; }
  public static RelayCommand RenameCommand { get; set; }
  public static RelayCommand ViewSelectedCommand { get; set; }

  public MediaItemVM(CoreVM coreVM, MediaItemS s) {
    _coreVM = coreVM;
    _s = s;
    CommentCommand = new(() => Comment(Current), () => Current != null, Res.IconNotification, "Comment");
    DeleteCommand = new(() => Delete(_coreVM.GetActive<MediaItemM>()), () => _coreVM.AnyActive<MediaItemM>());
    LoadByGeoNameCommand = new(LoadBy, Res.IconImageMultiple, "Load Media items");
    LoadByKeywordCommand = new(LoadBy, Res.IconImageMultiple, "Load Media items");
    LoadByPersonCommand = new(LoadBy, Res.IconImageMultiple, "Load Media items");
    LoadByPeopleOrSegmentsCommand = new(LoadByPeopleOrSegments, Res.IconImageMultiple, "Load Media items with selected People or Segments");
    RenameCommand = new(Rename, () => Current is RealMediaItemM, null, "Rename");
    ViewSelectedCommand = new(ViewSelected, CanViewSelected, Res.IconImageMultiple, "View selected");
  }

  private void Comment(MediaItemM mi) {
    var inputDialog = new InputDialog(
      "Comment",
      "Add a comment.",
      Res.IconNotification,
      mi.Comment,
      answer => answer.Length > 256
        ? "Comment is too long!"
        : string.Empty);

    if (Dialog.Show(inputDialog) == 1)
      _s.SetComment(mi, StringUtils.NormalizeComment(inputDialog.Answer));
  }

  private void LoadBy(object o) =>
    _coreVM.MediaItem.Views.LoadByTag(o);

  private void LoadByPeopleOrSegments() {
    var md = new MessageDialog(
      "Load Media items",
      "Do you want to load Media items from selected People or Segments?",
      Res.IconImageMultiple,
      true);

    md.Buttons = new DialogButton[] {
      new(md.SetResult(1, Res.IconPeople, "People"), true),
      new(md.SetResult(2, Res.IconSegment, "Segments"))
    };

    var result = Dialog.Show(md);
    if (result < 1) return;

    var items = result switch {
      1 => Core.S.Person.Selected.Items.ToArray(),
      2 => Core.S.Segment.Selected.Items.ToArray(),
      _ => Array.Empty<object>()
    };

    if (items.Length == 0) return;
    _coreVM.MediaItem.Views.LoadByTag(items);
  }

  private void ViewSelected() {
    var items = Views.Current.Selected.Items.ToList();
    _coreVM.MainWindow.IsInViewMode = true;
    _coreVM.MediaViewer.SetMediaItems(items, items[0]);
  }

  private bool CanViewSelected() =>
    Views.Current?.Selected.Items.Count > 1;

  public bool Delete(MediaItemM[] items) {
    if (items.Length == 0 || Dialog.Show(new MessageDialog(
          "Delete Confirmation",
          "Do you really want to delete {0} item{1}?".Plural(items.Length),
          MH.UI.Res.IconQuestion,
          true)) != 1) return false;

    _s.DeleteFromDrive(items);
    return true;
  }

  public void ReloadMetadata(RealMediaItemM[] items) {
    if (items.Length == 0 || Dialog.Show(new MessageDialog(
          "Reload metadata from files",
          "Do you really want to reload image metadata for {0} file{1}?".Plural(items.Length),
          MH.UI.Res.IconQuestion,
          true)) != 1) return;

    // TODO check async and maybe use the other ProgressBarDialog
    var progress = new ProgressBarAsyncDialog("Reloading metadata...", MH.UI.Res.IconImage, true, Environment.ProcessorCount);
    progress.Init(items, null, async mi => await _s.ReloadMetadata(mi), mi => mi.FilePath,
      delegate { _s.OnMetadataReloaded(items); });
    progress.Start();
    Dialog.Show(progress);
  }

  public void Rename() {
    var ext = Path.GetExtension(Current.FileName);
    var dlg = new InputDialog(
      "Rename",
      "Add a new name.",
      Res.IconNotification,
      Path.GetFileNameWithoutExtension(Current.FileName),
      answer => {
        var newFileName = answer + ext;

        if (Path.GetInvalidFileNameChars().Any(x => newFileName.IndexOf(x) != -1))
          return "New file name contains invalid character!";

        if (File.Exists(IOExtensions.PathCombine(Current.Folder.FullPath, newFileName)))
          return "New file name already exists!";

        return string.Empty;
      });

    if (Dialog.Show(dlg) != 1) return;
    _s.Rename((RealMediaItemM)Current, dlg.Answer + ext);
  }

  public void OnMetadataChanged(MediaItemM[] items) {
    CurrentGeoName?.OnPropertyChanged(nameof(CurrentGeoName.FullName));
    foreach (var mi in items)
      mi.SetInfoBox(true);
  }

  public Task ViewMediaItems(MediaItemM[] items, string name) =>
    Views.ViewMediaItems(items, name);
}