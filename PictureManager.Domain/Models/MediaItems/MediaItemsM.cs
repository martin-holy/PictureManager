using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Extensions;
using PictureManager.Domain.Database;
using PictureManager.Domain.Dialogs;
using PictureManager.Domain.HelperClasses;
using PictureManager.Domain.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Domain.Models.MediaItems;

public sealed class MediaItemsM : ObservableObject {
  private static readonly string[] _supportedExts = { ".jpg", ".jpeg", ".mp4" };
  private readonly MediaItemsDA _da;
  private MediaItemM _current;
  
  public MediaItemM Current { get => _current; set { _current = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurrentGeoName)); } }
  public GeoNameM CurrentGeoName => Current?.GeoLocation?.GeoName;
  public int ItemsCount => GetItemsCount();
  public int ModifiedItemsCount => GetModifiedCount();

  public static Action<MediaItemMetadata, bool> ReadMetadata { get; set; }
  public Func<ImageM, GeoNameM, bool> WriteMetadata { get; set; }

  public RelayCommand<object> DeleteCommand { get; }
  public RelayCommand<object> RenameCommand { get; }
  public RelayCommand<object> CommentCommand { get; }
  public RelayCommand<object> ReloadMetadataCommand { get; }
  public RelayCommand<FolderM> ReloadMetadataInFolderCommand { get; }
  public RelayCommand<object> RebuildVideoThumbnailsCommand { get; }

  public MediaItemsM(MediaItemsDA da) {
    _da = da;

    DeleteCommand = new(() => Delete(GetActive().ToArray()), () => GetActive().Any());
    RenameCommand = new(Rename, () => Current is RealMediaItemM);
    CommentCommand = new(() => Comment(Current), () => Current != null);

    ReloadMetadataCommand = new(
      () => ReloadMetadata(Core.MediaItemsViews.Current.Selected.Items.OfType<RealMediaItemM>().ToList()),
      () => Core.MediaItemsViews.Current?.Selected.Items.OfType<RealMediaItemM>().Any() == true);

    ReloadMetadataInFolderCommand = new(
      x => ReloadMetadata(x.GetMediaItems(Keyboard.IsShiftOn()).ToList()),
      x => x != null);

    RebuildVideoThumbnailsCommand = new(
      () => Core.VideoThumbsM.Create(GetActive(), true),
      () => GetActive().Any());
  }

  public void OnMetadataChanged(MediaItemM[] items) {
    UpdateModifiedCount();
    foreach (var mi in items) {
      mi.OnPropertyChanged(nameof(mi.DisplayKeywords));
      mi.OnPropertyChanged(nameof(mi.DisplayPeople));
      mi.SetInfoBox(true);
    }
  }

  public void UpdateItemsCount() => OnPropertyChanged(nameof(ItemsCount));

  public void UpdateModifiedCount() => OnPropertyChanged(nameof(ModifiedItemsCount));

  private int GetModifiedCount() =>
    Core.Db.Images.All.Count(x => x.IsOnlyInDb) +
    Core.Db.Videos.All.Count(x => x.IsOnlyInDb);

  private int GetItemsCount() =>
    Core.Db.Images.All.Count +
    Core.Db.Videos.All.Count;

  public bool Exists(MediaItemM mi) {
    if (mi == null || File.Exists(mi.FilePath)) return true;
    File.Delete(mi.FilePathCache);
    _da.ItemsDelete(new[] { mi });
    return false;
  }

  public bool Delete(MediaItemM[] items) {
    if (items.Length == 0) return false;
    if (Dialog.Show(new MessageDialog(
          "Delete Confirmation",
          "Do you really want to delete {0} item{1}?".Plural(items.Length),
          Res.IconQuestion,
          true)) != 1) return false;

    _da.ItemsDelete(items);
    return true;
  }

  /// <summary>
  /// Copy or Move MediaItems (Files, Cache and DB)
  /// </summary>
  /// <param name="mode"></param>
  /// <param name="items"></param>
  /// <param name="destFolder"></param>
  public void CopyMove(FileOperationMode mode, List<RealMediaItemM> items, FolderM destFolder) {
    var fop = new FileOperationDialogM(mode, false);
    fop.RunTask = Task.Run(() => {
      fop.LoadCts = new();
      var token = fop.LoadCts.Token;

      try {
        CopyMove(mode, items, destFolder, fop.Progress, token);
      }
      catch (Exception ex) {
        Tasks.RunOnUiThread(() => Dialog.Show(new ErrorDialogM(ex)));
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

  private void CopyMove(FileOperationMode mode, List<RealMediaItemM> items, FolderM destFolder,
    IProgress<object[]> progress, CancellationToken token) {
    var count = items.Count;
    var done = 0;
    var replaced = new List<MediaItemM>();

    foreach (var mi in items) {
      if (token.IsCancellationRequested)
        break;

      progress.Report(new object[]
        { Convert.ToInt32((double)done / count * 100), mi.Folder.FullPath, destFolder.FullPath, mi.FileName });

      var miNewFileName = mi.FileName;
      var destFilePath = IOExtensions.PathCombine(destFolder.FullPath, mi.FileName);

      // if the file with the same name exists in the destination
      // show dialog with options to Rename, Replace or Skip the file
      if (File.Exists(destFilePath)) {
        var result = FileOperationCollisionDialogM.Open(mi.FilePath, destFilePath, ref miNewFileName);

        if (result == CollisionResult.Skip) {
          Tasks.RunOnUiThread(() => Core.MediaItemsViews.Current.Selected.Set(mi, false));
          continue;
        }

        if (result == CollisionResult.Replace)
          replaced.Add(mi);
      }

      switch (mode) {
        case FileOperationMode.Copy:
          // create object copy
          var miCopy = _da.ItemCopy(mi, destFolder, miNewFileName);
          // copy MediaItem and cache on file system
          Directory.CreateDirectory(Path.GetDirectoryName(miCopy.FilePathCache) ?? throw new ArgumentNullException());
          File.Copy(mi.FilePath, miCopy.FilePath, true);
          File.Copy(mi.FilePathCache, miCopy.FilePathCache, true);

          if (mi.Segments != null)
            for (var i = 0; i < mi.Segments.Count; i++)
              File.Copy(mi.Segments[i].FilePathCache, miCopy.Segments[i].FilePathCache, true);

          break;

        case FileOperationMode.Move:
          var srcFilePath = mi.FilePath;
          var srcFilePathCache = mi.FilePathCache;
          var srcDirPathCache = Path.GetDirectoryName(mi.FilePathCache) ?? throw new ArgumentNullException();

          // DB
          _da.ItemMove(mi, destFolder, miNewFileName); // TODO mi rewrite

          // File System
          File.Delete(mi.FilePath);
          File.Move(srcFilePath, mi.FilePath);

          // Cache
          Directory.CreateDirectory(Path.GetDirectoryName(mi.FilePathCache) ?? throw new ArgumentNullException());
          // Thumbnail
          File.Delete(mi.FilePathCache);
          if (File.Exists(srcFilePathCache))
            File.Move(srcFilePathCache, mi.FilePathCache);
          // Segments
          foreach (var segment in mi.Segments ?? Enumerable.Empty<SegmentM>()) {
            File.Delete(segment.FilePathCache);
            var srcSegmentPath = Path.Combine(srcDirPathCache, $"segment_{segment.GetHashCode()}.jpg");
            if (File.Exists(srcSegmentPath))
              File.Move(srcSegmentPath, segment.FilePathCache);
          }

          break;
      }

      done++;
    }

    _da.ItemsDelete(replaced);
  }

  private void ReloadMetadata(List<RealMediaItemM> mediaItems) {
    if (mediaItems.Count == 0 ||
        Dialog.Show(new MessageDialog(
          "Reload metadata from files",
          "Do you really want to reload image metadata for {0} file{1}?".Plural(mediaItems.Count),
          Res.IconQuestion,
          true)) != 1) return;

    var items = mediaItems.ToArray();
    var progress = new ProgressBarAsyncDialog("Reloading metadata...", Res.IconImage, true, Environment.ProcessorCount);
    progress.Init(
      items,
      null,
      async mi => {
        // TODO check async and maybe use the other ProgressBarDialog
        var mim = new MediaItemMetadata(mi);
        ReadMetadata(mim, false);

        await Tasks.RunOnUiThread(async () => {
          if (mim.Success) await mim.FindRefs();
          _da.Modify(mi);
          mi.IsOnlyInDb = false;
        });
      },
      mi => mi.FilePath,
      delegate { _da.RaiseMetadataChanged(items.Cast<MediaItemM>().ToArray()); });

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
    _da.ItemRename((RealMediaItemM)Current, dlg.Answer + ext);
  }

  private void Comment(MediaItemM item) {
    var inputDialog = new InputDialog(
      "Comment",
      "Add a comment.",
      Res.IconNotification,
      item.Comment,
      answer => answer.Length > 256
        ? "Comment is too long!"
        : string.Empty);

    if (Dialog.Show(inputDialog) != 1) return;

    item.Comment = StringUtils.NormalizeComment(inputDialog.Answer);
    item.SetInfoBox(true);
    item.OnPropertyChanged();
    _da.Modify(item);
  }

  public MediaItemM[] GetActive() =>
    Core.MainWindowM.IsInViewMode
      ? Current == null
        ? Array.Empty<MediaItemM>()
        : new[] { Current }
      : Core.MediaItemsViews.Current == null
        ? Array.Empty<MediaItemM>()
        : Core.MediaItemsViews.Current.Selected.Items.ToArray();

  public static bool IsSupportedFileType(string filePath) =>
    _supportedExts.Any(x => x.Equals(Path.GetExtension(filePath), StringComparison.OrdinalIgnoreCase));
}