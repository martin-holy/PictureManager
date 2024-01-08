using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Extensions;
using PictureManager.Domain.Database;
using PictureManager.Domain.Dialogs;
using PictureManager.Domain.Extensions;
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
  public GeoNameM CurrentGeoName => Core.Db.MediaItemGeoLocation.GetBy(Current)?.GeoName;
  public int ItemsCount => GetItemsCount();
  public int ModifiedItemsCount => GetModifiedCount();

  public event EventHandler<ObjectEventArgs<RealMediaItemM[]>> MediaItemsOrientationChangedEvent = delegate { };
  public event EventHandler<ObjectEventArgs<MediaItemM[]>> MetadataChangedEvent = delegate { };
  public static Action<MediaItemMetadata, bool> ReadMetadata { get; set; }
  public Func<ImageM, GeoNameM, bool> WriteMetadata { get; set; }

  public RelayCommand<object> DeleteCommand { get; }
  public RelayCommand<object> RotateCommand { get; }
  public RelayCommand<object> RenameCommand { get; }
  public RelayCommand<object> CommentCommand { get; }
  public RelayCommand<object> ReloadMetadataCommand { get; }
  public RelayCommand<FolderM> ReloadMetadataInFolderCommand { get; }
  public RelayCommand<object> ViewModifiedCommand { get; }

  public MediaItemsM(MediaItemsDA da) {
    _da = da;
    MetadataChangedEvent += OnMetadataChanged;

    DeleteCommand = new(() => Delete(GetActive().ToArray()), () => GetActive().Any());
    RotateCommand = new(Rotate, () => GetActive().OfType<RealMediaItemM>().Any());
    RenameCommand = new(Rename, () => Current is RealMediaItemM);
    CommentCommand = new(Comment, () => Current != null);

    ReloadMetadataCommand = new(
      () => ReloadMetadata(Core.MediaItemsViews.Current.Selected.Items.OfType<RealMediaItemM>().ToList()),
      () => Core.MediaItemsViews.Current?.Selected.Items.OfType<RealMediaItemM>().Any() == true);

    ReloadMetadataInFolderCommand = new(
      x => ReloadMetadata(x.GetMediaItems(Keyboard.IsShiftOn()).ToList()),
      x => x != null);

    ViewModifiedCommand = new(ViewModified);
  }

  private void OnMetadataChanged(object sender, ObjectEventArgs<MediaItemM[]> e) =>
    UpdateModifiedCount();

  public void RaiseMetadataChanged(MediaItemM[] items) =>
    MetadataChangedEvent(this, new(items));

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

    var items = new[] { mi };
    SetCurrentAfterDelete(items);
    File.Delete(mi.FilePathCache);
    _da.ItemsDelete(items);

    return false;
  }

  private void SetCurrentAfterDelete(IList<MediaItemM> items) {
    var view = Core.MediaItemsViews.Current;
    Current = ListExtensions.GetNextOrPreviousItem(
      view != null
        ? view.FilteredItems
        : Core.MediaViewerM.MediaItems,
      items);
  }

  public bool Delete(MediaItemM[] items) {
    if (items.Length == 0) return false;
    if (Dialog.Show(new MessageDialog(
          "Delete Confirmation",
          "Do you really want to delete {0} item{1}?".Plural(items.Length),
          Res.IconQuestion,
          true)) != 1) return false;

    var rItems = items.OfType<RealMediaItemM>().ToArray();
    if (rItems.Any()) {
      SetCurrentAfterDelete(items);
      Core.FileOperationDelete(rItems.Select(x => x.FilePath).ToList(), true, false);
    }
      
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
      Current = ListExtensions.GetNextOrPreviousItem(Core.MediaItemsViews.Current.FilteredItems, mis);
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

  public void OnPersonRenamed(PersonM person) {
    UpdateInfoBox(Core.Db.Images.All, Where);
    UpdateInfoBox(Core.Db.Videos.All, Where);
    UpdateInfoBox(Core.Db.VideoClips.All, Where);
    UpdateInfoBox(Core.Db.VideoImages.All, Where);
    return;

    bool Where(MediaItemM mi) => mi.InfoBoxPeople != null && mi.People?.Contains(person) == true;
  }

  public void OnKeywordRenamed(KeywordM keyword) {
    UpdateInfoBox(Core.Db.Images.All, Where);
    UpdateInfoBox(Core.Db.Videos.All, Where);
    UpdateInfoBox(Core.Db.VideoClips.All, Where);
    UpdateInfoBox(Core.Db.VideoImages.All, Where);
    return;

    bool Where(MediaItemM mi) => mi.InfoBoxKeywords != null && mi.Keywords?.Contains(keyword) == true;
  }

  private static void UpdateInfoBox(IEnumerable<MediaItemM> items, Func<MediaItemM, bool> where) {
    foreach (var item in items.Where(where))
      item.SetInfoBox();
  }

  private void SetOrientation(RealMediaItemM[] mediaItems, MediaOrientation orientation) {
    foreach (var mi in mediaItems) {
      var newOrientation = mi.RotationAngle;

      if (mi is ImageM)
        switch (orientation) {
          case MediaOrientation.Rotate90: newOrientation += 90; break;
          case MediaOrientation.Rotate180: newOrientation += 180; break;
          case MediaOrientation.Rotate270: newOrientation += 270; break;
        }
      else if (mi is VideoM) // images have switched 90 and 270 angles and all application is made with this in mind
        // so I switched orientation just for video
        switch (orientation) {
          case MediaOrientation.Rotate90: newOrientation += 270; break;
          case MediaOrientation.Rotate180: newOrientation += 180; break;
          case MediaOrientation.Rotate270: newOrientation += 90; break;
        }

      if (newOrientation >= 360) newOrientation -= 360;

      switch (newOrientation) {
        case 0: mi.Orientation = (int)MediaOrientation.Normal; break;
        case 90: mi.Orientation = (int)MediaOrientation.Rotate90; break;
        case 180: mi.Orientation = (int)MediaOrientation.Rotate180; break;
        case 270: mi.Orientation = (int)MediaOrientation.Rotate270; break;
      }

      _da.Modify(mi);
      mi.SetThumbSize(true);
      File.Delete(mi.FilePathCache);
    }

    MediaItemsOrientationChangedEvent(this, new(mediaItems));
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
          mi.SetInfoBox();
        });
      },
      mi => mi.FilePath,
      delegate { RaiseMetadataChanged(items.Cast<MediaItemM>().ToArray()); });

    progress.Start();
    Dialog.Show(progress);
  }

  private void Rotate() {
    var rotation = Dialog.Show(new RotationDialogM());
    if (rotation == 0) return;

    SetOrientation(GetActive().OfType<RealMediaItemM>().ToArray(), (MediaOrientation)rotation);

    if (Core.MediaViewerM.IsVisible)
      Core.MediaViewerM.Current = Core.MediaViewerM.Current;
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

    Core.Db.MediaItems.ItemRename((RealMediaItemM)Current, dlg.Answer + ext);
  }

  public void Comment() {
    var inputDialog = new InputDialog(
      "Comment",
      "Add a comment.",
      Res.IconNotification,
      Current.Comment,
      answer => answer.Length > 256
        ? "Comment is too long!"
        : string.Empty);

    if (Dialog.Show(inputDialog) != 1) return;

    Current.Comment = StringUtils.NormalizeComment(inputDialog.Answer);
    Current.SetInfoBox();
    Current.OnPropertyChanged(nameof(Current.Comment));
    _da.Modify(Current);
  }

  public MediaItemM[] GetActive() =>
    Core.MainWindowM.IsInViewMode
      ? Current == null
        ? Array.Empty<MediaItemM>()
        : new[] { Current }
      : Core.MediaItemsViews.Current == null
        ? Array.Empty<MediaItemM>()
        : Core.MediaItemsViews.Current.Selected.Items.ToArray();

  public void SetMetadata(MediaItemM[] items, object item) {
    if (items.Length == 0) return;

    var count = 0;

    foreach (var mi in items) {
      var modified = true;

      switch (item) {
        case PersonM p:
          if (mi.Segments == null || !mi.Segments.Any(x => ReferenceEquals(x.Person, p)))
            mi.People = ListExtensions.Toggle(mi.People, p, true);
          break;

        case KeywordM k:
          mi.Keywords = KeywordsM.Toggle(mi.Keywords, k);
          break;

        case RatingTreeM r:
          mi.Rating = r.Rating.Value;
          break;

        case GeoNameM g:
          Core.Db.MediaItemGeoLocation.ItemUpdate(new(mi,
            Core.Db.GeoLocations.GetOrCreate(null, null, null, g).Result));
          break;

        default:
          modified = false;
          break;
      }

      if (!modified) continue;

      _da.Modify(mi);
      mi.SetInfoBox();
      count++;
    }

    if (count > 0) RaiseMetadataChanged(items);;
  }

  public static bool IsPanoramic(MediaItemM mi) =>
    mi.Orientation is (int)MediaOrientation.Rotate90 or (int)MediaOrientation.Rotate270
      ? mi.Height / (double)mi.Width > 16.0 / 9.0
      : mi.Width / (double)mi.Height > 16.0 / 9.0;

  private async void ViewModified() {
    Core.MediaItemsViews.AddView("Modified");
    await Core.MediaItemsViews.Current.LoadByTag(GetItems(x => x.IsOnlyInDb).ToArray());
  }

  public void OnSegmentsPersonChanged(SegmentM[] segments) {
    var items = segments.GetMediaItems().ToArray();

    foreach (var mi in items) {
      if (mi.People != null && mi.Segments != null) {
        foreach (var p in mi.Segments.Select(x => x.Person).Where(mi.People.Contains).ToArray())
          mi.People.Remove(p);

        if (!mi.People.Any())
          mi.People = null;
      }

      _da.Modify(mi);
      mi.SetInfoBox();
    }

    RaiseMetadataChanged(items);
  }

  public void OnSegmentsKeywordsChanged(SegmentM[] segments) {
    var items = segments.GetMediaItems().ToArray();

    foreach (var item in items)
      _da.Modify(item);

    RaiseMetadataChanged(items);
  }

  public static bool IsSupportedFileType(string filePath) =>
    _supportedExts.Any(x => x.Equals(Path.GetExtension(filePath), StringComparison.OrdinalIgnoreCase));

  public IEnumerable<MediaItemM> GetItems(Func<MediaItemM, bool> where) =>
    Core.Db.Images.All.Where(where)
      .Concat(Core.Db.Videos.All.Where(where))
      .Concat(Core.Db.VideoClips.All.Where(where))
      .Concat(Core.Db.VideoImages.All.Where(where));

  public IEnumerable<MediaItemM> GetItems(RatingM rating) =>
    GetItems(mi => mi.Rating == rating.Value);

  public IEnumerable<MediaItemM> GetItems(PersonM person) =>
    GetItems(mi =>
        mi.People?.Contains(person) == true ||
        mi.Segments?.Any(s => s.Person == person) == true)
      .OrderBy(mi => mi.FileName);

  public IEnumerable<MediaItemM> GetItems(KeywordM keyword, bool recursive) {
    var set = (recursive ? keyword.Flatten() : new[] { keyword }).ToHashSet();

    return GetItems(mi =>
      mi.Keywords?.Any(k => set.Contains(k)) == true ||
      mi.Segments?.Any(s => s.Keywords?.Any(k => set.Contains(k)) == true) == true);
  }

  public IEnumerable<MediaItemM> GetItems(GeoNameM geoName, bool recursive) {
    var set = (recursive ? geoName.Flatten() : new[] { geoName }).ToHashSet();

    return Core.Db.MediaItemGeoLocation.All
      .Where(x => x.Value.GeoName != null && set.Contains(x.Value.GeoName))
      .Select(x => x.Key)
      .OrderBy(mi => mi.FileName);
  }
}