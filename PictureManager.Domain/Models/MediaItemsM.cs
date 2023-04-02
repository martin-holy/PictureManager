using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.DataAdapters;
using PictureManager.Domain.Dialogs;
using PictureManager.Domain.Utils;

namespace PictureManager.Domain.Models {
  public sealed class MediaItemsM : ObservableObject {
    private readonly Core _core;
    private readonly SegmentsM _segmentsM;
    private readonly ViewersM _viewersM;

    private bool _isEditModeOn;
    private MediaItemM _current;

    public MediaItemsDataAdapter DataAdapter { get; set; }
    public HashSet<MediaItemM> ModifiedItems { get; } = new();
    public Dictionary<MediaItemM, ObservableCollection<ITreeItem>> MediaItemVideoClips { get; } = new();
    public MediaItemM Current { get => _current; set { _current = value; OnPropertyChanged(); } }
    public int MediaItemsCount => DataAdapter.All.Count;
    public int ModifiedItemsCount => ModifiedItems.Count;
    public bool IsEditModeOn { get => _isEditModeOn; set { _isEditModeOn = value; OnPropertyChanged(); } }

    public delegate Dictionary<string, string> FileOperationDelete(List<string> items, bool recycle, bool silent);

    public event EventHandler<ObjectEventArgs<MediaItemM>> MediaItemDeletedEventHandler = delegate { };
    public event EventHandler<ObjectEventArgs<MediaItemM[]>> MediaItemsDeletedEventHandler = delegate { };
    public event EventHandler<ObjectEventArgs<MediaItemM[]>> MediaItemsOrientationChangedEventHandler = delegate { };
    public event EventHandler MetadataChangedEventHandler = delegate { };
    public Func<MediaItemM, bool, Task<bool>> ReadMetadata { get; set; }
    public Func<MediaItemM, bool> WriteMetadata { get; set; }
    public FileOperationDelete FileOperationDeleteMethod { get; set; }

    public RelayCommand<object> DeleteCommand { get; }
    public RelayCommand<object> RotateCommand { get; }
    public RelayCommand<object> RenameCommand { get; }
    public RelayCommand<object> EditCommand { get; }
    public RelayCommand<object> SaveEditCommand { get; }
    public RelayCommand<object> CancelEditCommand { get; }
    public RelayCommand<object> CommentCommand { get; }
    public RelayCommand<object> ReloadMetadataCommand { get; }
    public RelayCommand<object> AddGeoNamesFromFilesCommand { get; }

    public MediaItemsM(Core core, SegmentsM segmentsM, ViewersM viewersM) {
      _core = core;
      _segmentsM = segmentsM;
      _viewersM = viewersM;

      DeleteCommand = new(
        Delete,
        () => GetActive().Any());

      RotateCommand = new(
        Rotate,
        () => GetActive().Any());

      RenameCommand = new(
        Rename,
        () => Current != null);

      EditCommand = new(
        () => IsEditModeOn = true,
        () => _core.ThumbnailsGridsM.Current?.FilteredItems.Count > 0 && !IsEditModeOn);

      SaveEditCommand = new(
        SaveEdit,
        () => IsEditModeOn && ModifiedItems.Count > 0);

      CancelEditCommand = new(
        CancelEdit,
        () => IsEditModeOn);

      CommentCommand = new(
        Comment,
        () => Current != null);

      ReloadMetadataCommand = new(
        () => ReloadMetadata(_core.ThumbnailsGridsM.Current.GetSelectedOrAll()),
        () => _core.ThumbnailsGridsM.Current?.FilteredItems.Count > 0);

      AddGeoNamesFromFilesCommand = new(
        () => AddGeoNamesFromFiles(Core.Settings.GeoNamesUserName),
        () => _core.ThumbnailsGridsM.Current?.FilteredItems.Count(x => x.IsSelected) > 0);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="all"></param>
    /// <param name="selected"></param>
    /// <returns>Returns next MediaItem from all after last in the selected or one before first or null</returns>
    public static MediaItemM GetNewCurrent(List<MediaItemM> all, List<MediaItemM> selected) {
      if (all == null || selected == null || selected.Count == 0) return null;

      var index = all.IndexOf(selected[^1]) + 1;
      if (index == all.Count)
        index = all.IndexOf(selected[0]) - 1;
      return index >= 0 ? all[index] : null;
    }

    public List<MediaItemM> GetMediaItems(PersonM person) =>
      DataAdapter.All.Values.Where(mi =>
          mi.People?.Contains(person) == true ||
          mi.Segments?.Any(s => s.Person == person) == true)
        .OrderBy(mi => mi.FileName).ToList();

    public List<MediaItemM> GetMediaItems(KeywordM keyword, bool recursive) {
      var keywords = new List<KeywordM> { keyword };
      if (recursive) Tree.GetThisAndItemsRecursive(keyword, ref keywords);
      var set = new HashSet<KeywordM>(keywords);

      return DataAdapter.All.Values
        .Where(mi => mi.Keywords?.Any(k => set.Contains(k)) == true
          || mi.Segments?.Any(s => s.Keywords?.Any(k => set.Contains(k)) == true) == true)
        .ToList();
    }

    public List<MediaItemM> GetMediaItems(GeoNameM geoName, bool recursive) {
      var geoNames = new List<GeoNameM> { geoName };
      if (recursive) Tree.GetThisAndItemsRecursive(geoName, ref geoNames);
      var set = new HashSet<GeoNameM>(geoNames);

      return DataAdapter.All.Values.Where(mi => set.Contains(mi.GeoName))
        .OrderBy(x => x.FileName).ToList();
    }

    public MediaItemM CopyTo(MediaItemM mi, FolderM folder, string fileName) {
      var copy = new MediaItemM(DataAdapter.GetNextId(), folder, fileName) {
        Width = mi.Width,
        Height = mi.Height,
        Orientation = mi.Orientation,
        Rating = mi.Rating,
        Comment = mi.Comment,
        GeoName = mi.GeoName,
        Lat = mi.Lat,
        Lng = mi.Lng
      };

      if (mi.People != null)
        copy.People = new(mi.People);

      if (mi.Keywords != null)
        copy.Keywords = new (mi.Keywords);

      if (mi.Segments != null) {
        copy.Segments = new();
        foreach (var segment in mi.Segments) {
          var sCopy = _segmentsM.GetCopy(segment);
          sCopy.MediaItem = copy;
          copy.Segments.Add(sCopy);
        }
      }

      copy.Folder.MediaItems.Add(copy);
      DataAdapter.All.Add(copy.Id, copy);
      OnPropertyChanged(nameof(MediaItemsCount));

      return copy;
    }

    public void MoveTo(MediaItemM mi, FolderM folder, string fileName) {
      // delete existing MediaItem if exists
      Delete(folder.MediaItems.SingleOrDefault(x => x.FileName.Equals(fileName)));

      mi.FileName = fileName;
      mi.Folder.MediaItems.Remove(mi);
      mi.Folder = folder;
      mi.Folder.MediaItems.Add(mi);

      DataAdapter.IsModified = true;
    }

    private void Rename(MediaItemM mi, string newFileName) {
      var oldFilePath = mi.FilePath;
      var oldFilePathCache = mi.FilePathCache;
      mi.FileName = newFileName;
      File.Move(oldFilePath, mi.FilePath);
      File.Move(oldFilePathCache, mi.FilePathCache);
      DataAdapter.IsModified = true;
    }

    public void Delete(MediaItemM item) {
      if (item == null) return;

      MediaItemDeletedEventHandler(this, new(item));

      item.People = null;
      item.Keywords = null;
      item.GeoName = null;

      // remove item from Folder
      item.Folder.MediaItems.Remove(item);
      //item.Folder = null;

      // remove from DB
      DataAdapter.All.Remove(item.Id);

      OnPropertyChanged(nameof(MediaItemsCount));

      SetModified(item, false);

      // set MediaItems table as modified
      DataAdapter.IsModified = true;
    }

    public void Delete(MediaItemM[] items) {
      foreach (var mi in items)
        Delete(mi);
    }

    private void Delete() {
      var items = GetActive();
      var count = items.Length;

      if (Core.DialogHostShow(new MessageDialog(
        "Delete Confirmation",
        $"Do you really want to delete {count} item{(count > 1 ? "s" : string.Empty)}?",
        Res.IconQuestion,
        true)) != 0) return;

      var currentThumbsGrid = _core.ThumbnailsGridsM.Current;
      var newCurrent = MediaItemsM.GetNewCurrent(currentThumbsGrid != null
        ? currentThumbsGrid.LoadedItems
        : _core.MediaViewerM.MediaItems,
        items.ToList());
      Delete(items, newCurrent);
    }

    public void Delete(MediaItemM[] items, MediaItemM newCurrent) {
      if (items.Length == 0) return;

      var files = new List<string>();
      var cache = new List<string>();

      foreach (var mi in items) {
        files.Add(mi.FilePath);
        cache.Add(mi.FilePathCache);
        Delete(mi);
      }

      FileOperationDeleteMethod.Invoke(files, true, false);
      cache.ForEach(File.Delete);

      Current = newCurrent;
      MediaItemsDeletedEventHandler(this, new(items));
    }

    private void SetModified(MediaItemM mi, bool value) {
      if (value) {
        ModifiedItems.Add(mi);
        DataAdapter.IsModified = true;
      }
      else
        ModifiedItems.Remove(mi);

      OnPropertyChanged(nameof(ModifiedItemsCount));
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
          Core.DialogHostShow(new ErrorDialogM(ex));
        }
      }).ContinueWith(_ => Core.RunOnUiThread(() => fop.Result = 1));

      _ = Core.DialogHostShow(fop);

      if (mode == FileOperationMode.Move) {
        _core.ThumbnailsGridsM.Current.RemoveSelected();
        _ = _core.ThumbnailsGridsM.Current?.ThumbsGridReloadItems();
      }
    }

    private void CopyMove(FileOperationMode mode, List<MediaItemM> items, FolderM destFolder, IProgress<object[]> progress, CancellationToken token) {
      var count = items.Count;
      var done = 0;

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
          var result = FileOperationCollisionDialogM.Show(mi.FilePath, destFilePath, ref miNewFileName);

          if (result == CollisionResult.Skip) {
            Core.RunOnUiThread(() => _core.ThumbnailsGridsM.Current.SetSelected(mi, false));
            continue;
          }
        }

        switch (mode) {
          case FileOperationMode.Copy:
            // create object copy
            var miCopy = CopyTo(mi, destFolder, miNewFileName);
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
            MoveTo(mi, destFolder, miNewFileName);

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
              var srcSegmentPath = Path.Combine(srcDirPathCache, $"segment_{segment.Id}.jpg");
              if (File.Exists(srcSegmentPath))
                File.Move(srcSegmentPath, segment.FilePathCache);
            }

            break;
        }

        done++;
      }
    }

    public MediaItemM AddNew(FolderM folder, string fileName, bool isNew, bool readMetadata) {
      var mi = new MediaItemM(DataAdapter.GetNextId(), folder, fileName, isNew);
      DataAdapter.All.Add(mi.Id, mi);
      OnPropertyChanged(nameof(MediaItemsCount));
      folder.MediaItems.Add(mi);

      if (readMetadata)
        _ = ReadMetadata(mi, false);

      return mi;
    }

    public async Task<List<MediaItemM>> GetMediaItemsForLoadAsync(IReadOnlyCollection<MediaItemM> mediaItems, IReadOnlyCollection<FolderM> folders) {
      var items = new List<MediaItemM>();

      if (mediaItems != null)
        // filter out items if directory or file not exists or Viewer can not see items
        items = await VerifyAccessibilityOfMediaItemsAsync(mediaItems);

      if (folders != null)
        items = await GetMediaItemsFromFoldersAsync(folders);

      foreach (var mi in items)
        mi.SetThumbSize();

      return items;
    }

    private async Task<List<MediaItemM>> GetMediaItemsFromFoldersAsync(IReadOnlyCollection<FolderM> folders) {
      var output = new List<MediaItemM>();

      await Task.Run(() => {
        foreach (var folder in folders) {
          if (!Directory.Exists(folder.FullPath)) continue;
          var folderMediaItems = new List<MediaItemM>();
          var hiddenMediaItems = new List<MediaItemM>();

          // add MediaItems from current Folder to dictionary for faster search
          var fmis = folder.MediaItems.ToDictionary(x => x.FileName);

          foreach (var file in Directory.EnumerateFiles(folder.FullPath, "*.*", SearchOption.TopDirectoryOnly)) {
            if (!Imaging.IsSupportedFileType(file)) continue;

            // check if the MediaItem is already in DB, if not put it there
            var fileName = Path.GetFileName(file);
            fmis.TryGetValue(fileName, out var inDbFile);
            inDbFile ??= AddNew(folder, fileName, true, false);

            if (!_viewersM.CanViewerSee(inDbFile)) {
              hiddenMediaItems.Add(inDbFile);
              continue;
            }
            folderMediaItems.Add(inDbFile);
          }

          output.AddRange(folderMediaItems.OrderBy(x => x.FileName));

          // remove MediaItems deleted outside of this application
          foreach (var fmi in folder.MediaItems.ToArray()) {
            if (folderMediaItems.Contains(fmi) || hiddenMediaItems.Contains(fmi)) continue;
            Delete(fmi);
          }
        }
      });

      return output;
    }

    private async Task<List<MediaItemM>> VerifyAccessibilityOfMediaItemsAsync(IReadOnlyCollection<MediaItemM> items) {
      var output = new List<MediaItemM>();

      await Task.Run(() => {
        var folders = items.Select(x => x.Folder).Distinct();
        var foldersSet = new HashSet<int>();

        foreach (var folder in folders) {
          if (!_viewersM.CanViewerSeeContentOf(folder)) continue;
          if (!Directory.Exists(folder.FullPath)) continue;
          foldersSet.Add(folder.Id);
        }

        foreach (var mi in items) {
          if (!foldersSet.Contains(mi.Folder.Id)) continue;
          if (!_viewersM.CanViewerSee(mi)) continue;
          if (!File.Exists(mi.FilePath)) continue;
          output.Add(mi);
        }
      });

      return output;
    }

    public void UpdateInfoBoxWithPerson(PersonM person) {
      foreach (var mi in DataAdapter.All.Values
                 .Where(mi => mi.InfoBoxPeople != null && mi.People?.Contains(person) == true))
        mi.SetInfoBox();
    }

    public void UpdateInfoBoxWithKeyword(KeywordM keyword) {
      foreach (var mi in DataAdapter.All.Values
                 .Where(mi => mi.InfoBoxKeywords != null && mi.Keywords?.Contains(keyword) == true))
        mi.SetInfoBox();
    }

    public void RemovePersonFromMediaItems(PersonM person) {
      foreach (var mi in DataAdapter.All.Values.Where(mi => mi.People?.Contains(person) == true)) {
        mi.People = ListExtensions.Toggle(mi.People, person, true);
        DataAdapter.IsModified = true;
      }
    }

    public void RemoveKeywordFromMediaItems(KeywordM keyword) {
      foreach (var mi in DataAdapter.All.Values.Where(mi => mi.Keywords?.Contains(keyword) == true)) {
        mi.Keywords = KeywordsM.Toggle(mi.Keywords, keyword);
        DataAdapter.IsModified = true;
      }
    }

    public bool TryWriteMetadata(MediaItemM mediaItem) {
      if (mediaItem.IsOnlyInDb) return true;
      try {
        return WriteMetadata(mediaItem) ? true : throw new("Error writing metadata");
      }
      catch (Exception ex) {
        _core.LogError(ex, $"Metadata will be saved just in Database. {mediaItem.FilePath}");
        // set MediaItem as IsOnlyInDb to not save metadata to file, but keep them just in DB
        mediaItem.IsOnlyInDb = true;
        return false;
      }
    }

    public void AddGeoNamesFromFiles(string geoNamesUserName) {
      if (!GeoNamesM.IsGeoNamesUserNameInSettings(geoNamesUserName)) return;

      var progress = new ProgressBarDialog("Adding GeoNames ...", true, 1);
      progress.AddEvents(
        _core.ThumbnailsGridsM.Current.FilteredItems.Where(x => x.IsSelected).ToArray(),
        null,
        // action
        async mi => {
          if (mi.Lat == null || mi.Lng == null) _ = await ReadMetadata(mi, true);
          if (mi.Lat == null || mi.Lng == null) return;

          var lastGeoName = _core.GeoNamesM.InsertGeoNameHierarchy((double)mi.Lat, (double)mi.Lng, geoNamesUserName);
          if (lastGeoName == null) return;

          mi.GeoName = lastGeoName;
          TryWriteMetadata(mi);
          await Core.RunOnUiThread(() => {
            mi.SetInfoBox();
            DataAdapter.IsModified = true;
          });
        },
        mi => mi.FilePath,
        // onCompleted
        delegate {
          Current?.GeoName?.OnPropertyChanged(nameof(Current.GeoName.FullName));
        });

      progress.Start();
      Core.DialogHostShow(progress);
    }

    public void RebuildThumbnails(object source, bool recursive) {
      var mediaItems = source switch {
        FolderM folder => folder.GetMediaItems(recursive),
        List<MediaItemM> items => items,
        _ => _core.ThumbnailsGridsM.Current.GetSelectedOrAll(),
      };

      var progress = new ProgressBarDialog("Rebuilding thumbnails ...", true, Environment.ProcessorCount);
      progress.AddEvents(
        mediaItems.ToArray(),
        null,
        (mi) => {
          mi.SetThumbSize(true);
          File.Delete(mi.FilePathCache);
        },
        mi => mi.FilePath,
        delegate {
          _ = _core.ThumbnailsGridsM.Current?.ThumbsGridReloadItems();
        });

      progress.Start();
      Core.DialogHostShow(progress);
    }

    public void SetOrientation(MediaItemM[] mediaItems, MediaOrientation orientation) {
      var progress = new ProgressBarDialog("Changing orientation ...", true, Environment.ProcessorCount);
      progress.AddEvents(
        mediaItems,
        null,
        // action
        mi => {
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

          TryWriteMetadata(mi);
          mi.SetThumbSize(true);
          File.Delete(mi.FilePathCache);
        },
        mi => mi.FilePath,
        // onCompleted
        (_, _) => MediaItemsOrientationChangedEventHandler(this, new(mediaItems)));

      progress.Start();
      Core.DialogHostShow(progress);
    }

    public void SaveEdit() {
      var progress = new ProgressBarDialog("Saving metadata ...", true, Environment.ProcessorCount);
      progress.AddEvents(
        ModifiedItems.ToArray(),
        null,
        // action
        async mi => {
          TryWriteMetadata(mi);
          await Core.RunOnUiThread(() => SetModified(mi, false));
        },
        mi => mi.FilePath,
        // onCompleted
        (_, e) => {
          if (e.Cancelled)
            CancelEdit();
          else
            IsEditModeOn = false;

          _core.StatusPanelM.OnPropertyChanged(nameof(_core.StatusPanelM.FileSize));
        });

      progress.Start();
      Core.DialogHostShow(progress);
    }

    public void CancelEdit() {
      var progress = new ProgressBarDialog("Reloading metadata ...", false, Environment.ProcessorCount);
      progress.AddEvents(
        ModifiedItems.ToArray(),
        null,
        // action
        async mi => {
          await ReadMetadata(mi, false);

          await Core.RunOnUiThread(() => {
            SetModified(mi, false);
            mi.SetInfoBox();
          });
        },
        mi => mi.FilePath,
        // onCompleted
        (_, _) => {
          MetadataChangedEventHandler(this, EventArgs.Empty);
          IsEditModeOn = false;
        });

      progress.Start();
      Core.DialogHostShow(progress);
    }

    public void ReloadMetadata(List<MediaItemM> mediaItems, bool updateInfoBox = false) {
      var progress = new ProgressBarDialog("Reloading metadata ...", true, Environment.ProcessorCount);
      progress.AddEvents(
        mediaItems.ToArray(),
        null,
        // action
        async (mi) => {
          await ReadMetadata(mi, false);

          // set info box just for loaded media items
          if (updateInfoBox)
            await Core.RunOnUiThread(mi.SetInfoBox);
        },
        mi => mi.FilePath,
        // onCompleted
        (_, _) => MetadataChangedEventHandler(this, EventArgs.Empty));

      progress.Start();
      Core.DialogHostShow(progress);
    }

    private void Rotate() {
      var rotation = (MediaOrientation)Core.DialogHostShow(new RotationDialogM());
      if (rotation == MediaOrientation.Normal) return;

      SetOrientation(GetActive(), rotation);
    }

    public async void Rename() {
      var inputDialog = new InputDialog(
        "Rename",
        "Add a new name.",
        Res.IconNotification,
        Path.GetFileNameWithoutExtension(Current.FileName),
        answer => {
          var newFileName = answer + Path.GetExtension(Current.FileName);

          if (Path.GetInvalidFileNameChars().Any(x => newFileName.IndexOf(x) != -1))
            return "New file name contains invalid character!";

          if (File.Exists(IOExtensions.PathCombine(Current.Folder.FullPath, newFileName)))
            return "New file name already exists!";

          return string.Empty;
        });
        
      if (Core.DialogHostShow(inputDialog) != 0) return;

      try {
        Rename(Current, inputDialog.Answer + Path.GetExtension(Current.FileName));
        if (_core.ThumbnailsGridsM.Current != null) {
          _core.ThumbnailsGridsM.Current.FilteredItemsSetInPlace(Current);
          await _core.ThumbnailsGridsM.Current.ThumbsGridReloadItems();
        }
        OnPropertyChanged(nameof(Current));
      }
      catch (Exception ex) {
        _core.LogError(ex);
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

      if (Core.DialogHostShow(inputDialog) != 0) return;

      Current.Comment = StringUtils.NormalizeComment(inputDialog.Answer);
      Current.SetInfoBox();
      Current.OnPropertyChanged(nameof(Current.Comment));
      TryWriteMetadata(Current);
      DataAdapter.IsModified = true;
    }

    public MediaItemM[] GetActive() =>
      _core.MainWindowM.IsFullScreen
        ? Current == null
          ? Array.Empty<MediaItemM>()
          : new[] { Current }
        : _core.ThumbnailsGridsM.Current == null
          ? Array.Empty<MediaItemM>()
          : _core.ThumbnailsGridsM.Current.SelectedItems.ToArray();

    public void SetMetadata(object item) {
      var items = GetActive();
      if (items.Length == 0) return;

      var count = 0;

      foreach (var mi in items) {
        var modified = true;

        switch (item) {
          case PersonM p:
            mi.People = ListExtensions.Toggle(mi.People, p, true);
            break;

          case KeywordM k:
            mi.Keywords = KeywordsM.Toggle(mi.Keywords, k);
            break;

          case RatingTreeM r:
            mi.Rating = r.Value;
            break;

          case GeoNameM g:
            mi.GeoName = g;
            break;

          default:
            modified = false;
            break;
        }

        if (!modified) continue;

        SetModified(mi, true);
        mi.SetInfoBox();
        count++;
      }

      if (count > 0)
        MetadataChangedEventHandler(this, EventArgs.Empty);
    }
  }
}
