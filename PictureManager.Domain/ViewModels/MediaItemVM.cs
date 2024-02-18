using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Extensions;
using PictureManager.Domain.Dialogs;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models;
using PictureManager.Domain.Models.MediaItems;
using PictureManager.Domain.Services;
using PictureManager.Domain.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Domain.ViewModels;

public sealed class MediaItemVM : ObservableObject {
  private readonly MediaItemS _s;
  private MediaItemM _current;
  private int _modifiedItemsCount;
  private int _itemsCount;

  public static IImageSourceConverter<MediaItemM> ThumbConverter { get; set; }

  public MediaItemM Current { get => _current; set { _current = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentGeoName)); } }
  public GeoNameM CurrentGeoName => Current?.GeoLocation?.GeoName;
  public int ModifiedItemsCount { get => _modifiedItemsCount; set { _modifiedItemsCount = value; OnPropertyChanged(); } }
  public int ItemsCount { get => _itemsCount; set { _itemsCount = value; OnPropertyChanged(); } }

  public static RelayCommand CommentCommand { get; set; }
  public static RelayCommand DeleteCommand { get; set; }
  public static RelayCommand RenameCommand { get; set; }

  public MediaItemVM(CoreVM coreVM, MediaItemS model) {
    _s = model;
    CommentCommand = new(() => Comment(Current), () => Current != null, Res.IconNotification, "Comment");
    DeleteCommand = new(() => Delete(coreVM.GetActive<MediaItemM>()), () => coreVM.AnyActive<MediaItemM>());
    RenameCommand = new(Rename, () => Current is RealMediaItemM, null, "Rename");
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

  public void CopyMove(FileOperationMode mode, List<RealMediaItemM> items, FolderM destFolder) {
    var fop = new FileOperationDialogM(mode, false);
    fop.RunTask = Task.Run(async () => {
      fop.LoadCts = new();
      var token = fop.LoadCts.Token;

      try {
        await _s.CopyMove(mode, items, destFolder, fop.Progress, token);
      }
      catch (Exception ex) {
        await Tasks.RunOnUiThread(() => Dialog.Show(new ErrorDialogM(ex)));
      }
    }).ContinueWith(_ => Tasks.RunOnUiThread(() => fop.Result = 1));

    _ = Dialog.Show(fop);

    if (mode == FileOperationMode.Move) {
      var mis = items.Cast<MediaItemM>().ToList();
      Current = Core.MediaItemsViews.Current.FilteredItems.GetNextOrPreviousItem(mis);
      // TODO add VirtualMediaItems
      Core.MediaItemsViews.Current.Remove(mis, true);
    }
  }

  public bool Delete(MediaItemM[] items) {
    if (items.Length == 0 || Dialog.Show(new MessageDialog(
          "Delete Confirmation",
          "Do you really want to delete {0} item{1}?".Plural(items.Length),
          Res.IconQuestion,
          true)) != 1) return false;

    _s.Delete(items);
    return true;
  }

  public void ReloadMetadata(RealMediaItemM[] items) {
    if (items.Length == 0 || Dialog.Show(new MessageDialog(
          "Reload metadata from files",
          "Do you really want to reload image metadata for {0} file{1}?".Plural(items.Length),
          Res.IconQuestion,
          true)) != 1) return;

    // TODO check async and maybe use the other ProgressBarDialog
    var progress = new ProgressBarAsyncDialog("Reloading metadata...", Res.IconImage, true, Environment.ProcessorCount);
    progress.Init(items, null, async mi => await _s.ReloadMetadata(mi), mi => mi.FilePath, delegate { _s.OnMetadataReloaded(items); });
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
}