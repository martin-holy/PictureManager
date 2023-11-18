using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Extensions;
using PictureManager.Domain.Database;
using PictureManager.Domain.DataViews;
using PictureManager.Domain.Dialogs;
using PictureManager.Domain.HelperClasses;
using PictureManager.Domain.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Domain.Models;

public sealed class MediaItemsM : ObservableObject {
  private MediaItemM _current;

  public MediaItemsDataAdapter DataAdapter { get; set; }
  public static HashSet<MediaItemM> ThumbIgnoreCache { get; } = new();
  public MediaItemM Current { get => _current; set { _current = value; OnPropertyChanged(); } }
  public int MediaItemsCount => DataAdapter.All.Count;
  public int ModifiedItemsCount => DataAdapter.All.Count(x => x.IsOnlyInDb);

  public event EventHandler<ObjectEventArgs<MediaItemM[]>> MediaItemsOrientationChangedEvent = delegate { };
  public event EventHandler MetadataChangedEvent = delegate { };
  public Action<MediaItemMetadata, bool> ReadMetadata { get; set; }
  public Func<MediaItemM, bool> WriteMetadata { get; set; }

  public RelayCommand<object> CompressCommand { get; }
  public RelayCommand<object> DeleteCommand { get; }
  public RelayCommand<object> RotateCommand { get; }
  public RelayCommand<object> RenameCommand { get; }
  public RelayCommand<MediaItemsView> ImagesToVideoCommand { get; }
  public RelayCommand<object> SaveToFilesCommand { get; }
  public RelayCommand<object> CommentCommand { get; }
  public RelayCommand<object> ResizeImagesCommand { get; }
  public RelayCommand<object> ReloadMetadataCommand { get; }
  public RelayCommand<object> AddGeoNamesFromFilesCommand { get; }
  public RelayCommand<FolderM> ReloadMetadataInFolderCommand { get; }
  public RelayCommand<object> RebuildThumbnailsCommand { get; }
  public RelayCommand<object> ViewModifiedCommand { get; }

  public MediaItemsM(MediaItemsDataAdapter da) {
    DataAdapter = da;
    DataAdapter.ItemCreatedEvent += OnItemCreated;
    DataAdapter.ItemRenamedEvent += OnItemRenamed;
    DataAdapter.ItemDeletedEvent += OnItemDeleted;

    CompressCommand = new(Compress, () => GetActive().Any());
    DeleteCommand = new(Delete, () => GetActive().Any());
    RotateCommand = new(Rotate, () => GetActive().Any());
    RenameCommand = new(Rename, () => Current != null);

    ResizeImagesCommand = new(
      () => Dialog.Show(new ResizeImagesDialogM(Core.MediaItemsViews.Current.GetSelectedOrAll())),
      () => Core.MediaItemsViews.Current?.FilteredItems.Count > 0);

    SaveToFilesCommand = new(SaveToFiles);
    CommentCommand = new(Comment, () => Current != null);

    ReloadMetadataCommand = new(
      () => ReloadMetadata(Core.MediaItemsViews.Current?.Selected.Items.ToList()),
      () => Core.MediaItemsViews.Current?.Selected.Items.Count > 0);

    AddGeoNamesFromFilesCommand = new(
      () => AddGeoNamesFromFiles(Core.Settings.GeoNamesUserName),
      () => Core.MediaItemsViews.Current?.Selected.Items.Count > 0);

    ReloadMetadataInFolderCommand = new(
      x => ReloadMetadata(x.GetMediaItems(Keyboard.IsShiftOn())),
      x => x != null);

    RebuildThumbnailsCommand = new(
      x => RebuildThumbnails(x, Keyboard.IsShiftOn()),
      x => x is FolderM || Core.MediaItemsViews.Current?.FilteredItems.Count > 0);

    ImagesToVideoCommand = new(
      ImagesToVideo, 
      view => view?.FilteredItems.Count(x => x.IsSelected && x.MediaType == MediaType.Image) > 1);

    ViewModifiedCommand = new(ViewModified);
  }

  private void OnItemCreated(object sender, ObjectEventArgs<MediaItemM> e) =>
    OnPropertyChanged(nameof(MediaItemsCount));
    
  private void OnItemRenamed(object sender, ObjectEventArgs<MediaItemM> e) =>
    OnPropertyChanged(nameof(Current));

  private void OnItemDeleted(object o, ObjectEventArgs<MediaItemM> e) {
    OnPropertyChanged(nameof(MediaItemsCount));
    OnPropertyChanged(nameof(ModifiedItemsCount));
  }

  private void ImagesToVideo(MediaItemsView view) {
    Dialog.Show(new ImagesToVideoDialogM(
      view.FilteredItems.Where(x => x.IsSelected && x.MediaType == MediaType.Image),
      (folder, fileName) => {
        var mi = DataAdapter.ItemCreate(folder, fileName);
        var mim = new MediaItemMetadata(mi);
        ReadMetadata(mim, false);
        mi.SetThumbSize();
        view.LoadedItems.Add(mi);
        view.SoftLoad(view.LoadedItems, true, true);
      })
    );
  }

  private void Compress() {
    Dialog.Show(new CompressDialogM(
      GetActive().Where(x => x.MediaType == MediaType.Image).ToList(),
      Core.Settings.JpegQualityLevel));

    OnPropertyChanged(nameof(ModifiedItemsCount));
  }

  public bool Exists(MediaItemM mi) {
    if (mi == null || File.Exists(mi.FilePath)) return true;

    var items = new[] { mi };
    SetCurrentAfterDelete(items);
    File.Delete(mi.FilePathCache);
    DataAdapter.ItemsDelete(items);

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

  private void Delete() {
    var items = GetActive().ToList();
    var count = items.Count;
    if (count == 0) return;

    if (Dialog.Show(new MessageDialog(
          "Delete Confirmation",
          $"Do you really want to delete {count} item{(count > 1 ? "s" : string.Empty)}?",
          Res.IconQuestion,
          true)) != 1) return;

    SetCurrentAfterDelete(items);
    Core.FileOperationDelete(items.Select(x => x.FilePath).ToList(), true, false);
    foreach (var c in items.Select(x => x.FilePathCache)) File.Delete(c);
    DataAdapter.ItemsDelete(items);
  }

  /// <summary>
  /// Copy or Move MediaItems (Files, Cache and DB)
  /// </summary>
  /// <param name="mode"></param>
  /// <param name="items"></param>
  /// <param name="destFolder"></param>
  public void CopyMove(FileOperationMode mode, List<MediaItemM> items, FolderM destFolder) {
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
      Current = ListExtensions.GetNextOrPreviousItem(Core.MediaItemsViews.Current.FilteredItems, items);
      Core.MediaItemsViews.Current.Remove(items, true);
    }
  }

  private void CopyMove(FileOperationMode mode, List<MediaItemM> items, FolderM destFolder, IProgress<object[]> progress, CancellationToken token) {
    var count = items.Count;
    var done = 0;
    var replaced = new List<MediaItemM>();

    foreach (var mi in items) {
      if (token.IsCancellationRequested)
        break;

      progress.Report(new object[]
        {Convert.ToInt32((double) done / count * 100), mi.Folder.FullPath, destFolder.FullPath, mi.FileName});

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
          var miCopy = DataAdapter.ItemCopy(mi, destFolder, miNewFileName);
          // copy MediaItem and cache on file system
          Directory.CreateDirectory(Path.GetDirectoryName(miCopy.FilePathCache) ?? throw new ArgumentNullException());
          File.Copy(mi.FilePath, miCopy.FilePath, true);
          File.Copy(mi.FilePathCache, miCopy.FilePathCache, true);

          if (mi.Segments != null)
            for (int i = 0; i < mi.Segments.Count; i++)
              File.Copy(mi.Segments[i].FilePathCache, miCopy.Segments[i].FilePathCache, true);
            
          break;

        case FileOperationMode.Move:
          var srcFilePath = mi.FilePath;
          var srcFilePathCache = mi.FilePathCache;
          var srcDirPathCache = Path.GetDirectoryName(mi.FilePathCache) ?? throw new ArgumentNullException();

          // DB
          DataAdapter.ItemMove(mi, destFolder, miNewFileName);

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

    DataAdapter.ItemsDelete(replaced);
  }

  public void UpdateInfoBoxWithPerson(PersonM person) {
    foreach (var mi in DataAdapter.All
               .Where(mi => mi.InfoBoxPeople != null && mi.People?.Contains(person) == true))
      mi.SetInfoBox();
  }

  public void UpdateInfoBoxWithKeyword(KeywordM keyword) {
    foreach (var mi in DataAdapter.All
               .Where(mi => mi.InfoBoxKeywords != null && mi.Keywords?.Contains(keyword) == true))
      mi.SetInfoBox();
  }

  public void RemovePersonFromMediaItems(PersonM person) {
    foreach (var mi in DataAdapter.All.Where(mi => mi.People?.Contains(person) == true)) {
      mi.People = ListExtensions.Toggle(mi.People, person, true);
      DataAdapter.Modify(mi);
    }
  }

  public void RemoveKeywordFromMediaItems(KeywordM keyword) {
    foreach (var mi in DataAdapter.All.Where(mi => mi.Keywords?.Contains(keyword) == true)) {
      mi.Keywords = KeywordsM.Toggle(mi.Keywords, keyword);
      DataAdapter.Modify(mi);
    }
  }

  public bool TryWriteMetadata(MediaItemM mi) {
    try {
      if (!WriteMetadata(mi)) throw new("Error writing metadata");
      mi.IsOnlyInDb = false;
    }
    catch (Exception ex) {
      Log.Error(ex, $"Metadata will be saved just in database. {mi.FilePath}");
      mi.IsOnlyInDb = true;
    }

    DataAdapter.IsModified = true;
    return !mi.IsOnlyInDb;
  }

  public void AddGeoNamesFromFiles(string geoNamesUserName) {
    if (!GeoNamesM.IsGeoNamesUserNameInSettings(geoNamesUserName)) return;

    var progress = new ProgressBarDialog("Adding GeoNames ...", Res.IconLocationCheckin, true, 1);
    progress.AddEvents(
      Core.MediaItemsViews.Current.FilteredItems.Where(x => x.IsSelected).ToArray(),
      null,
      // action
      async mi => {
        if (mi.Lat == null || mi.Lng == null) {
          var mim = new MediaItemMetadata(mi);
          ReadMetadata(mim, true);
          if (mim.Success)
            await Tasks.RunOnUiThread(() => mim.FindGeoName());
        }
        if (mi.Lat == null || mi.Lng == null) return;

        var lastGeoName = Core.GeoNamesM.InsertGeoNameHierarchy((double)mi.Lat, (double)mi.Lng, geoNamesUserName);
        if (lastGeoName == null) return;

        mi.GeoName = lastGeoName;
        await Tasks.RunOnUiThread(() => {
          mi.SetInfoBox();
          DataAdapter.Modify(mi);
        });
      },
      mi => mi.FilePath,
      // onCompleted
      delegate {
        Current?.GeoName?.OnPropertyChanged(nameof(Current.GeoName.FullName));
        MetadataChangedEvent(this, EventArgs.Empty);
      });

    progress.Start();
    Dialog.Show(progress);
  }

  public void RebuildThumbnails(object source, bool recursive) {
    var mediaItems = source switch {
      FolderM folder => folder.GetMediaItems(recursive),
      List<MediaItemM> items => items,
      _ => Core.MediaItemsViews.Current.GetSelectedOrAll(),
    };

    foreach (var mi in mediaItems) {
      mi.SetThumbSize(true);
      ThumbIgnoreCache.Add(mi);
      File.Delete(mi.FilePathCache);
    }

    Core.MediaItemsViews.Current.ReWrapAll();
  }

  public void SetOrientation(MediaItemM[] mediaItems, MediaOrientation orientation) {
    foreach (var mi in mediaItems) {
      var newOrientation = mi.RotationAngle;

      if (mi.MediaType == MediaType.Image) {
        switch (orientation) {
          case MediaOrientation.Rotate90: newOrientation += 90; break;
          case MediaOrientation.Rotate180: newOrientation += 180; break;
          case MediaOrientation.Rotate270: newOrientation += 270; break;
        }
      }
      else if (mi.MediaType == MediaType.Video) {
        // images have switched 90 and 270 angles and all application is made with this in mind
        // so I switched orientation just for video
        switch (orientation) {
          case MediaOrientation.Rotate90: newOrientation += 270; break;
          case MediaOrientation.Rotate180: newOrientation += 180; break;
          case MediaOrientation.Rotate270: newOrientation += 90; break;
        }
      }

      if (newOrientation >= 360) newOrientation -= 360;

      switch (newOrientation) {
        case 0: mi.Orientation = (int)MediaOrientation.Normal; break;
        case 90: mi.Orientation = (int)MediaOrientation.Rotate90; break;
        case 180: mi.Orientation = (int)MediaOrientation.Rotate180; break;
        case 270: mi.Orientation = (int)MediaOrientation.Rotate270; break;
      }

      DataAdapter.Modify(mi);
      mi.SetThumbSize(true);
      ThumbIgnoreCache.Add(mi);
      File.Delete(mi.FilePathCache);
    }

    MediaItemsOrientationChangedEvent(this, new(mediaItems));
  }

  public void SaveToFiles() {
    var mediaItems = Core.MediaItemsViews.Current?.FilteredItems
      .Where(x => x.IsSelected && x.MediaType == MediaType.Image)
      .ToArray();

    if (mediaItems == null || !mediaItems.Any()) return;
    if (Dialog.Show(new MessageDialog(
          "Save metadata to files",
          "Do you really want to save image metadata to {0} file{1}?".Plural(mediaItems.Length),
          Res.IconQuestion,
          true)) != 1) return;

    var progress = new ProgressBarDialog("Saving metadata to files...", Res.IconImage, true, Environment.ProcessorCount);
    progress.AddEvents(mediaItems, null, mi => TryWriteMetadata(mi), mi => mi.FilePath, null);
    progress.Start();
    Dialog.Show(progress);
    Core.MediaItemsStatusBarM.OnPropertyChanged(nameof(Core.MediaItemsStatusBarM.FileSize));
    OnPropertyChanged(nameof(ModifiedItemsCount));
  }

  public void ReloadMetadata(List<MediaItemM> mediaItems) {
    if (mediaItems.Count == 0 ||
        Dialog.Show(new MessageDialog(
          "Reload metadata from files",
          "Do you really want to reload image metadata for {0} file{1}?".Plural(mediaItems.Count),
          Res.IconQuestion,
          true)) != 1) return;

    var progress = new ProgressBarDialog("Reloading metadata...", Res.IconImage, true, Environment.ProcessorCount);
    progress.AddEvents(
      mediaItems.ToArray(),
      null,
      async mi => {
        var mim = new MediaItemMetadata(mi);
        ReadMetadata(mim, false);

        await Tasks.RunOnUiThread(() => {
          if (mim.Success) mim.FindRefs();
          DataAdapter.IsModified = true;
          mi.IsOnlyInDb = false;
          mi.SetInfoBox();
        });
      },
      mi => mi.FilePath,
      delegate {
        MetadataChangedEvent(this, EventArgs.Empty);
      });

    progress.Start();
    Dialog.Show(progress);
  }

  private void Rotate() {
    var rotation = Dialog.Show(new RotationDialogM());
    if (rotation == 0) return;

    SetOrientation(GetActive(), (MediaOrientation)rotation);

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

    try {
      DataAdapter.ItemRename(Current, dlg.Answer + ext);
    }
    catch (Exception ex) {
      Log.Error(ex);
    }
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
    DataAdapter.Modify(Current);
  }

  public MediaItemM[] GetActive() =>
    Core.MainWindowM.IsFullScreen
      ? Current == null
        ? Array.Empty<MediaItemM>()
        : new[] { Current }
      : Core.MediaItemsViews.Current == null
        ? Array.Empty<MediaItemM>()
        : Core.MediaItemsViews.Current.Selected.Items.ToArray();

  public void SetMetadata(object item) {
    var items = GetActive();
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
          mi.GeoName = g;
          break;

        default:
          modified = false;
          break;
      }

      if (!modified) continue;

      DataAdapter.Modify(mi);
      mi.SetInfoBox();
      count++;
    }

    if (count > 0) MetadataChangedEvent(this, EventArgs.Empty);
  }

  public static bool IsPanoramic(MediaItemM mi) =>
    mi.Orientation is (int)MediaOrientation.Rotate90 or (int)MediaOrientation.Rotate270
      ? mi.Height / (double)mi.Width > 16.0 / 9.0
      : mi.Width / (double)mi.Height > 16.0 / 9.0;

  private async void ViewModified() {
    Core.MediaItemsViews.AddView("Modified");
    await Core.MediaItemsViews.Current.LoadByTag(DataAdapter.All.Where(x => x.IsOnlyInDb).ToArray());
  }

  public void OnSegmentsPersonChanged(IEnumerable<MediaItemM> items) {
    foreach (var mi in items) {
      if (mi.People != null && mi.Segments != null) {
        foreach (var p in mi.Segments.Select(x => x.Person).Where(mi.People.Contains).ToArray())
          mi.People.Remove(p);

        if (!mi.People.Any())
          mi.People = null;
      }

      DataAdapter.Modify(mi);
      mi.SetInfoBox();
    }

    OnPropertyChanged(nameof(ModifiedItemsCount));
  }
}