using PictureManager.Domain.DataAdapters;
using PictureManager.Domain.Extensions;
using PictureManager.Domain.Utils;
using SimpleDB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Directory = System.IO.Directory;

namespace PictureManager.Domain.Models {
  public sealed class MediaItems : ObservableObject, ITable {
    private readonly Core _core;

    public DataAdapter DataAdapter { get; }
    public List<IRecord> All { get; } = new();
    public Dictionary<int, MediaItem> AllDic { get; set; }
    public delegate Dictionary<string, string> FileOperationDelete(List<string> items, bool recycle, bool silent);

    private bool _isEditModeOn;
    private int _mediaItemsCount;
    private string _positionSlashCount;
    private MediaItem _current;
    private ThumbnailsGrid _currentThumbsGrid;

    public MediaItem Current {
      get => _current;
      set {
        _current = value;
        if (ThumbsGrid != null && ThumbsGrid.Current != value)
          ThumbsGrid.Current = value;
        OnPropertyChanged(nameof(Current));
        OnPropertyChanged(nameof(ActiveFileSize));
      }
    }

    public ThumbnailsGrid ThumbsGrid {
      get => _currentThumbsGrid;
      set {
        _currentThumbsGrid = value;
        OnPropertyChanged(nameof(ThumbsGrid));
        OnPropertyChanged(nameof(ActiveFileSize));
      }
    }

    public string ActiveFileSize {
      get {
        try {
          var size = Current == null
            ? ThumbsGrid?.SelectedItems.Sum(mi => new FileInfo(mi.FilePath).Length)
            : new FileInfo(Current.FilePath).Length;

          return size == null || size == 0 ? string.Empty : Extension.FileSizeToString((long)size);
        }
        catch {
          return string.Empty;
        }
      }
    }

    public bool IsEditModeOn { get => _isEditModeOn; set { _isEditModeOn = value; OnPropertyChanged(); } }
    public int MediaItemsCount { get => _mediaItemsCount; set { _mediaItemsCount = value; OnPropertyChanged(); } }
    public string PositionSlashCount { get => _positionSlashCount; set { _positionSlashCount = value; OnPropertyChanged(); } }
    public int ModifiedCount => ModifiedItems.Count;
    public List<MediaItem> ModifiedItems { get; } = new();
    public ObservableCollection<ThumbnailsGrid> ThumbnailsGrids { get; } = new();
    public delegate CollisionResult CollisionResolver(string srcFilePath, string destFilePath, ref string destFileName);

    public MediaItems(Core core) {
      _core = core;
      DataAdapter = new MediaItemsDataAdapter(core, this);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="all"></param>
    /// <param name="selected"></param>
    /// <returns>Returns next MediaItem from all after last in the selected or one before first or null</returns>
    public static MediaItem GetNewCurrent(List<MediaItem> all, List<MediaItem> selected) {
      if (all == null || selected == null || selected.Count == 0) return null;

      var index = all.IndexOf(selected[^1]) + 1;
      if (index == all.Count)
        index = all.IndexOf(selected[0]) - 1;
      return index >= 0 ? all[index] : null;
    }

    public void Delete(List<MediaItem> items, FileOperationDelete fileOperationDelete) {
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

    public void Delete(MediaItem item) {
      if (item == null) return;

      // remove Segments
      if (item.Segments != null) {
        foreach (var segment in item.Segments.ToArray()) {
          // removing segment here prevents removing segment from Segments.Delete
          // and setting DB table as modified multiple times
          item.Segments.Remove(segment);
          _core.Segments.Delete(segment);
        }
        item.Segments = null;
      }

      item.People = null;
      item.Keywords = null;

      // remove item from Folder
      item.Folder.MediaItems.Remove(item);
      item.Folder = null;

      // remove GeoName
      item.GeoName = null;

      // remove from ThumbnailsGrids
      foreach (var thumbnailsGrid in ThumbnailsGrids)
        thumbnailsGrid.Remove(item);

      // remove from DB
      All.Remove(item);

      MediaItemsCount--;

      SetModified(item, false);

      // set MediaItems table as modified
      DataAdapter.IsModified = true;
    }

    public void SetModified(MediaItem mi, bool value) {
      if (mi.IsModified == value) return;
      mi.IsModified = value;
      if (value) {
        ModifiedItems.Add(mi);
        DataAdapter.IsModified = true;
      }
      else
        ModifiedItems.Remove(mi);

      OnPropertyChanged(nameof(ModifiedCount));
    }

    public static void CopyMove(FileOperationMode mode, List<MediaItem> items, Folder destFolder,
      IProgress<object[]> progress, CollisionResolver collisionResolver, CancellationToken token) {
      var count = items.Count;
      var done = 0;

      foreach (var mi in items) {
        if (token.IsCancellationRequested)
          break;

        progress.Report(new object[]
          {Convert.ToInt32((double) done / count * 100), mi.Folder.FullPath, destFolder.FullPath, mi.FileName});

        var miNewFileName = mi.FileName;
        var destFilePath = Extension.PathCombine(destFolder.FullPath, mi.FileName);

        // if the file with the same name exists in the destination
        // show dialog with options to Rename, Replace or Skip the file
        if (File.Exists(destFilePath)) {
          var result = collisionResolver.Invoke(mi.FilePath, destFilePath, ref miNewFileName);

          if (result == CollisionResult.Skip) {
            Core.Instance.RunOnUiThread(() => Core.Instance.MediaItems.ThumbsGrid.SetSelected(mi, false));
            continue;
          }
        }

        switch (mode) {
          case FileOperationMode.Copy:
          // create object copy
          var miCopy = mi.CopyTo(destFolder, miNewFileName);
          // copy MediaItem and cache on file system
          Directory.CreateDirectory(Path.GetDirectoryName(miCopy.FilePathCache) ?? throw new ArgumentNullException());
          File.Copy(mi.FilePath, miCopy.FilePath, true);
          File.Copy(mi.FilePathCache, miCopy.FilePathCache, true);

          if (mi.Segments != null)
            for (int i = 0; i < mi.Segments.Count; i++)
              File.Copy(mi.Segments[i].CacheFilePath, miCopy.Segments[i].CacheFilePath, true);
          break;

          case FileOperationMode.Move:
          var srcFilePath = mi.FilePath;
          var srcFilePathCache = mi.FilePathCache;
          var srcDirPathCache = Path.GetDirectoryName(mi.FilePathCache) ?? throw new ArgumentNullException();

          // DB
          mi.MoveTo(destFolder, miNewFileName);

          // File System
          File.Delete(mi.FilePath);
          File.Move(srcFilePath, mi.FilePath);

          // Cache
          Directory.CreateDirectory(Path.GetDirectoryName(mi.FilePathCache) ?? throw new ArgumentNullException());
          // Thumbnail
          File.Delete(mi.FilePathCache);
          File.Move(srcFilePathCache, mi.FilePathCache);
          // Segments
          foreach (var segment in mi.Segments ?? Enumerable.Empty<Segment>()) {
            File.Delete(segment.CacheFilePath);
            File.Move(Path.Combine(srcDirPathCache, $"segment_{segment.Id}.jpg"), segment.CacheFilePath);
          }
          break;
        }

        done++;
      }
    }

    public async Task<List<MediaItem>> GetMediaItemsFromFoldersAsync(IReadOnlyCollection<Folder> folders, CancellationToken token) {
      var output = new List<MediaItem>();

      await Task.Run(() => {
        foreach (var folder in folders.Where(x => !x.IsHidden)) {
          if (token.IsCancellationRequested) break;
          if (!Directory.Exists(folder.FullPath)) continue;
          var folderMediaItems = new List<MediaItem>();
          var hiddenMediaItems = new List<MediaItem>();

          // add MediaItems from current Folder to dictionary for faster search
          var fmis = folder.MediaItems.ToDictionary(x => x.FileName);

          foreach (var file in Directory.EnumerateFiles(folder.FullPath, "*.*", SearchOption.TopDirectoryOnly)) {
            if (token.IsCancellationRequested) break;
            if (!Imaging.IsSupportedFileType(file)) continue;

            // check if the MediaItem is already in DB, if not put it there
            var fileName = Path.GetFileName(file) ?? string.Empty;
            fmis.TryGetValue(fileName, out var inDbFile);
            if (inDbFile == null) {
              inDbFile = new MediaItem(DataAdapter.GetNextId(), folder, fileName, true);
              All.Add(inDbFile);
              folder.MediaItems.Add(inDbFile);
            }
            if (!_core.CanViewerSee(inDbFile)) {
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

    public static async Task<List<MediaItem>> VerifyAccessibilityOfMediaItemsAsync(IReadOnlyCollection<MediaItem> items, CancellationToken token) {
      var output = new List<MediaItem>();

      await Task.Run(() => {
        var folders = items.Select(x => x.Folder).Distinct();
        var foldersSet = new HashSet<int>();

        foreach (var folder in folders) {
          if (token.IsCancellationRequested) break;
          if (!Core.Instance.CanViewerSeeContentOfThisFolder(folder)) continue;
          if (!Directory.Exists(folder.FullPath)) continue;
          foldersSet.Add(folder.Id);
        }

        foreach (var mi in items) {
          if (token.IsCancellationRequested) break;
          if (!foldersSet.Contains(mi.Folder.Id)) continue;
          if (!File.Exists(mi.FilePath)) continue;
          if (!Core.Instance.CanViewerSee(mi)) continue;
          output.Add(mi);
        }
      }, token);

      return output;
    }

    public ThumbnailsGrid AddThumbnailsGridModel() {
      var grid = new ThumbnailsGrid();
      ThumbnailsGrids.Add(grid);
      ThumbsGrid = grid;

      grid.OnSelectionChanged += (o, e) => OnPropertyChanged(nameof(ActiveFileSize));

      return grid;
    }

    public void RemovePersonFromMediaItems(PersonM person) {
      foreach (var mi in All.Cast<MediaItem>().Where(mi => mi.People != null && mi.People.Contains(person))) {
        mi.People = Extension.Toggle(mi.People, person, true);
        DataAdapter.IsModified = true;
      }
    }

    public void RemoveKeywordsFromMediaItems(IEnumerable<KeywordM> keywords) {
      var set = new HashSet<KeywordM>(keywords);
      foreach (var mi in All.Cast<MediaItem>().Where(mi => mi.Keywords != null)) {
        foreach (var keyword in mi.Keywords.Where(set.Contains).ToArray()) {
          mi.Keywords = Extension.Toggle(mi.Keywords, keyword, true);
          DataAdapter.IsModified = true;
        }
      }
    }
  }
}
