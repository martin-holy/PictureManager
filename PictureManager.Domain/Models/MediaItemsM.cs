using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Extensions;
using PictureManager.Domain.Utils;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class MediaItemsM : ObservableObject {
    private readonly Core _core;
    private readonly SegmentsM _segmentsM;
    private readonly ViewersM _viewersM;

    private bool _isEditModeOn;
    private MediaItemM _current;

    public DataAdapter DataAdapter { get; set; }
    public List<MediaItemM> All { get; } = new();
    public Dictionary<int, MediaItemM> AllDic { get; set; }
    public HashSet<MediaItemM> ModifiedItems { get; } = new();

    public MediaItemM Current {
      get => _current;
      set {
        _current = value;
        if (_core.ThumbnailsGridsM.Current != null && _core.ThumbnailsGridsM.Current.CurrentMediaItem != value)
          _core.ThumbnailsGridsM.Current.CurrentMediaItem = value;
        OnPropertyChanged();
      }
    }

    public int MediaItemsCount => All.Count;
    public int ModifiedItemsCount => ModifiedItems.Count;
    public bool IsEditModeOn { get => _isEditModeOn; set { _isEditModeOn = value; OnPropertyChanged(); } }

    public delegate Dictionary<string, string> FileOperationDelete(List<string> items, bool recycle, bool silent);
    public delegate CollisionResult CollisionResolver(string srcFilePath, string destFilePath, ref string destFileName);

    public event EventHandler<ObjectEventArgs<MediaItemM>> MediaItemDeletedEventHandler = delegate { };
    public Func<MediaItemM, bool, Task<bool>> ReadMetadata { get; set; }
    public Func<MediaItemM, bool> WriteMetadata { get; set; }

    public MediaItemsM(Core core, SegmentsM segmentsM, ViewersM viewersM) {
      _core = core;
      _segmentsM = segmentsM;
      _viewersM = viewersM;
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
      All.Where(mi =>
          mi.People?.Contains(person) == true ||
          mi.Segments?.Any(s => s.Person == person) == true)
        .OrderBy(mi => mi.FileName).ToList();

    public List<MediaItemM> GetMediaItems(KeywordM keyword, bool recursive) {
      var keywords = new List<KeywordM> { keyword };
      if (recursive) Tree.GetThisAndItemsRecursive(keyword, ref keywords);
      var set = new HashSet<KeywordM>(keywords);

      return All.Where(mi => mi.Keywords?.Any(k => set.Contains(k)) == true).ToList();
    }

    public List<MediaItemM> GetMediaItems(GeoNameM geoName, bool recursive) {
      var geoNames = new List<GeoNameM> { geoName };
      if (recursive) Tree.GetThisAndItemsRecursive(geoName, ref geoNames);
      var set = new HashSet<GeoNameM>(geoNames);

      return All.Where(mi => set.Contains(mi.GeoName))
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
      All.Add(copy);
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

    public void Rename(MediaItemM mi, string newFileName) {
      var oldFilePath = mi.FilePath;
      var oldFilePathCache = mi.FilePathCache;
      mi.FileName = newFileName;
      File.Move(oldFilePath, mi.FilePath);
      File.Move(oldFilePathCache, mi.FilePathCache);
      DataAdapter.IsModified = true;
    }

    public void Delete(MediaItemM item) {
      if (item == null) return;

      item.People = null;
      item.Keywords = null;
      item.GeoName = null;

      // remove item from Folder
      item.Folder.MediaItems.Remove(item);
      item.Folder = null;

      // remove from DB
      All.Remove(item);

      MediaItemDeletedEventHandler(this, new(item));
      OnPropertyChanged(nameof(MediaItemsCount));

      SetModified(item, false);

      // set MediaItems table as modified
      DataAdapter.IsModified = true;
    }

    public void Delete(MediaItemM[] items) {
      foreach (var mi in items)
        Delete(mi);
    }

    public void Delete(List<MediaItemM> items, FileOperationDelete fileOperationDelete) {
      if (items.Count == 0) return;

      var files = new List<string>();
      var cache = new List<string>();

      foreach (var mi in items) {
        files.Add(mi.FilePath);
        cache.Add(mi.FilePathCache);
        Delete(mi);
      }

      fileOperationDelete.Invoke(files, true, false);
      cache.ForEach(File.Delete);
    }

    public void SetModified(MediaItemM mi, bool value) {
      if (value) {
        ModifiedItems.Add(mi);
        DataAdapter.IsModified = true;
      }
      else
        ModifiedItems.Remove(mi);

      OnPropertyChanged(nameof(ModifiedItemsCount));
    }

    public void CopyMove(FileOperationMode mode, List<MediaItemM> items, FolderM destFolder,
      IProgress<object[]> progress, CollisionResolver collisionResolver, CancellationToken token) {
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
          var result = collisionResolver.Invoke(mi.FilePath, destFilePath, ref miNewFileName);

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
            File.Move(srcFilePathCache, mi.FilePathCache);
            // Segments
            foreach (var segment in mi.Segments ?? Enumerable.Empty<SegmentM>()) {
              File.Delete(segment.FilePathCache);
              File.Move(Path.Combine(srcDirPathCache, $"segment_{segment.Id}.jpg"), segment.FilePathCache);
            }

            break;
        }

        done++;
      }
    }

    public async Task<List<MediaItemM>> GetMediaItemsForLoadAsync(IReadOnlyCollection<MediaItemM> mediaItems, IReadOnlyCollection<FolderM> folders, CancellationToken token) {
      var items = new List<MediaItemM>();

      if (mediaItems != null)
        // filter out items if directory or file not exists or Viewer can not see items
        items = await VerifyAccessibilityOfMediaItemsAsync(mediaItems, token);

      if (folders != null)
        items = await GetMediaItemsFromFoldersAsync(folders, token);

      foreach (var mi in items)
        mi.SetThumbSize();

      return items;
    }

    public async Task<List<MediaItemM>> GetMediaItemsFromFoldersAsync(IReadOnlyCollection<FolderM> folders, CancellationToken token) {
      var output = new List<MediaItemM>();

      await Task.Run(() => {
        foreach (var folder in folders) {
          if (token.IsCancellationRequested) break;
          if (!Directory.Exists(folder.FullPath)) continue;
          var folderMediaItems = new List<MediaItemM>();
          var hiddenMediaItems = new List<MediaItemM>();

          // add MediaItems from current Folder to dictionary for faster search
          var fmis = folder.MediaItems.ToDictionary(x => x.FileName);

          foreach (var file in Directory.EnumerateFiles(folder.FullPath, "*.*", SearchOption.TopDirectoryOnly)) {
            if (token.IsCancellationRequested) break;
            if (!Imaging.IsSupportedFileType(file)) continue;

            // check if the MediaItem is already in DB, if not put it there
            var fileName = Path.GetFileName(file);
            fmis.TryGetValue(fileName, out var inDbFile);
            if (inDbFile == null) {
              inDbFile = new(DataAdapter.GetNextId(), folder, fileName, true);
              All.Add(inDbFile);
              OnPropertyChanged(nameof(MediaItemsCount));
              folder.MediaItems.Add(inDbFile);
            }
            if (!_viewersM.CanViewerSee(inDbFile)) {
              hiddenMediaItems.Add(inDbFile);
              continue;
            }
            folderMediaItems.Add(inDbFile);
          }

          if (token.IsCancellationRequested) break;

          output.AddRange(folderMediaItems.OrderBy(x => x.FileName));

          // remove MediaItems deleted outside of this application
          foreach (var fmi in folder.MediaItems.ToArray()) {
            if (folderMediaItems.Contains(fmi) || hiddenMediaItems.Contains(fmi)) continue;
            Delete(fmi);
          }
        }
      }, token);

      return output;
    }

    public async Task<List<MediaItemM>> VerifyAccessibilityOfMediaItemsAsync(IReadOnlyCollection<MediaItemM> items, CancellationToken token) {
      var output = new List<MediaItemM>();

      await Task.Run(() => {
        var folders = items.Select(x => x.Folder).Distinct();
        var foldersSet = new HashSet<int>();

        foreach (var folder in folders) {
          if (token.IsCancellationRequested) break;
          if (!_viewersM.CanViewerSeeContentOf(folder)) continue;
          if (!Directory.Exists(folder.FullPath)) continue;
          foldersSet.Add(folder.Id);
        }

        foreach (var mi in items) {
          if (token.IsCancellationRequested) break;
          if (!foldersSet.Contains(mi.Folder.Id)) continue;
          if (!_viewersM.CanViewerSee(mi)) continue;
          if (!File.Exists(mi.FilePath)) continue;
          output.Add(mi);
        }
      }, token);

      return output;
    }

    public void RemovePersonFromMediaItems(PersonM person) {
      foreach (var mi in All.Where(mi => mi.People?.Contains(person) == true)) {
        mi.People = ListExtensions.Toggle(mi.People, person, true);
        DataAdapter.IsModified = true;
      }
    }

    public void RemoveKeywordFromMediaItems(KeywordM keyword) {
      foreach (var mi in All.Where(mi => mi.Keywords?.Contains(keyword) == true)) {
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
  }
}
