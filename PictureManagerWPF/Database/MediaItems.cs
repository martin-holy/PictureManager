﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls;
using PictureManager.Dialogs;
using PictureManager.Utils;
using Directory = System.IO.Directory;
using PictureManager.ViewModel;

namespace PictureManager.Database {
  public class MediaItems : INotifyPropertyChanged, ITable {
    public TableHelper Helper { get; set; }
    public List<MediaItem> All { get; } = new List<MediaItem>();

    private MediaItem _current;
    private bool _isEditModeOn;
    private int _selected;
    private int? _indexOfCurrent;

    public List<MediaItem> LoadedItems { get; } = new List<MediaItem>();
    public ObservableCollection<MediaItem> FilteredItems { get; } = new ObservableCollection<MediaItem>();
    public ObservableCollection<ObservableCollection<MediaItem>> SplittedItems { get; } = new ObservableCollection<ObservableCollection<MediaItem>>();

    public MediaItem Current {
      get => _current;
      set {
        if (_current != null) SetSelected(_current, false);
        _current = value;
        if (_current != null) SetSelected(_current, true);
        _indexOfCurrent = value == null ? null : (int?) FilteredItems.IndexOf(value);
        OnPropertyChanged();
        OnPropertyChanged(nameof(PositionSlashCount));
        App.Core.AppInfo.CurrentMediaItem = value;
      }
    }

    public bool IsEditModeOn { get => _isEditModeOn; set { _isEditModeOn = value; OnPropertyChanged(); } }
    public int Selected { get => _selected; set { _selected = value; OnPropertyChanged(); } }
    public string PositionSlashCount => $"{(Current == null ? string.Empty : $"{_indexOfCurrent + 1}/")}{FilteredItems.Count}";

    public int ModifiedCount => ModifiedItems.Count;
    public List<MediaItem> ModifiedItems = new List<MediaItem>();

    private CancellationTokenSource _loadCts;
    private Task _loadTask;

    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    ~MediaItems() {
      if (_loadCts != null) {
        _loadCts.Dispose();
        _loadCts = null;
      }
    }

    public void NewFromCsv(string csv) {
      // ID|Folder|Name|Width|Height|Orientation|Rating|Comment|GeoName|People|Keywords
      var props = csv.Split('|');
      if (props.Length != 11) return;
      var id = int.Parse(props[0]);
      AddRecord(new MediaItem(id, null, props[2]) {
        Csv = props,
        Width = props[3].IntParseOrDefault(0),
        Height = props[4].IntParseOrDefault(0),
        Orientation = props[5].IntParseOrDefault(1),
        Rating = props[6].IntParseOrDefault(0),
        Comment = string.IsNullOrEmpty(props[7]) ? null : props[7]
      });
    }

    public void LinkReferences() {
      foreach (var mi in All) {
        // reference to Folder and back reference from Folder to MediaItems
        mi.Folder = App.Core.Folders.AllDic[int.Parse(mi.Csv[1])];
        mi.Folder.MediaItems.Add(mi);

        // reference to People and back reference from Person to MediaItems
        if (!string.IsNullOrEmpty(mi.Csv[9])) {
          var ids = mi.Csv[9].Split(',');
          mi.People = new List<Person>(ids.Length);
          foreach (var personId in ids) {
            var p = App.Core.People.AllDic[int.Parse(personId)];
            p.MediaItems.Add(mi);
            mi.People.Add(p);
          }
        }

        // reference to Keywords and back reference from Keyword to MediaItems
        if (!string.IsNullOrEmpty(mi.Csv[10])) {
          var ids = mi.Csv[10].Split(',');
          mi.Keywords = new List<Keyword>(ids.Length);
          foreach (var keywordId in ids) {
            var k = App.Core.Keywords.AllDic[int.Parse(keywordId)];
            k.MediaItems.Add(mi);
            mi.Keywords.Add(k);
          }
        }

        // reference to GeoName
        if (!string.IsNullOrEmpty(mi.Csv[8])) {
          mi.GeoName = App.Core.GeoNames.AllDic[int.Parse(mi.Csv[8])];
          mi.GeoName.MediaItems.Add(mi);
        }

        // csv array is not needed any more
        mi.Csv = null;
      }
    }

    public void SaveToFile() {
      Helper.SaveToFile(All);
    }

    public void LoadFromFile() {
      All.Clear();
      Helper.LoadFromFile();
    }

    public void AddRecord(MediaItem record) {
      All.Add(record);
    }

    public void Delete(MediaItem item) {
      if (item == null) return;
        
      // remove People
      if (item.People != null) {
        foreach (var person in item.People)
          person.MediaItems.Remove(item);
        item.People = null;
      }

      // remove Keywords
      if (item.Keywords != null) {
        foreach (var keyword in item.Keywords)
          keyword.MediaItems.Remove(item);
        item.Keywords = null;
      }

      // remove item from Folder
      item.Folder.MediaItems.Remove(item);
      item.Folder = null;

      // remove GeoName
      item.GeoName?.MediaItems.Remove(item);
      item.GeoName = null;

      // remove from DB
      All.Remove(item);

      App.Core.AppInfo.MediaItemsCount--;

      // set MediaItems table as modified
      Helper.IsModified = true;
    }

    public void Delete(MediaItem[] items) {
      var progress = new ProgressBarDialog(App.WMain, false, 1, "Removing Media Items from database ...");
      progress.AddEvents(items, null, Delete, mi => mi.FilePath, null);
      progress.StartDialog();
    }

    public void SetSelected(MediaItem mi, bool value) {
      if (mi.IsSelected == value) return;
      mi.IsSelected = value;
      if (value) Selected++; else Selected--;
    }

    public void SetModified(MediaItem mi, bool value) {
      if (mi.IsModified == value) return;
      mi.IsModified = value;
      if (value)
        ModifiedItems.Add(mi);
      else
        ModifiedItems.Remove(mi);

      OnPropertyChanged(nameof(ModifiedCount));
    }

    public List<MediaItem> GetSelectedOrAll() {
      var mediaItems = FilteredItems.Where(x => x.IsSelected).ToList();
      return mediaItems.Count == 0 ? FilteredItems.ToList() : mediaItems;
    }

    public void SelectAll() {
      Current = null;
      foreach (var mi in FilteredItems)
        SetSelected(mi, true);
    }

    public void DeselectAll() {
      Current = null;
      foreach (var mi in LoadedItems)
        SetSelected(mi, false);
    }

    public void SelectNotModified() {
      Current = null;
      foreach (var mi in FilteredItems) {
        SetSelected(mi, false);
        if (!mi.IsModified)
          SetSelected(mi, true);
      }
    }

    public void SetMetadata(object tag) {
      foreach (var mi in FilteredItems.Where(x => x.IsSelected)) {
        SetModified(mi, true);

        switch (tag) {
          case Person p: {
            if (p.IsMarked) {
              if (mi.People == null)
                mi.People = new List<Person>();
              mi.People.Add(p);
              p.MediaItems.Add(mi);
            }
            else {
              mi.People.Remove(p);
              p.MediaItems.Remove(mi);
              if (mi.People.Count == 0)
                mi.People = null;
            }
            break;
          }
          case Keyword k: {
            if (!k.IsMarked && mi.Keywords != null) {
              mi.Keywords.Remove(k);
              k.MediaItems.Remove(mi);
              if (mi.Keywords.Count == 0)
                mi.Keywords = null;
              break;
            }
            
            if (mi.Keywords != null) {
              // skip if any Parent of MediaItem Keywords is marked Keyword
              var skip = false;
              foreach (var miKeyword in mi.Keywords) {
                var tmpMik = miKeyword;
                while (tmpMik.Parent is Keyword parent) {
                  tmpMik = parent;
                  if (!parent.Id.Equals(k.Id)) continue;
                  skip = true;
                  break;
                }
              }

              if (skip) break;

              // remove possible redundant keywords 
              // example: if marked keyword is "Weather/Sunny" keyword "Weather" is redundant
              foreach (var miKeyword in mi.Keywords.ToArray()) {
                var tmpMarkedK = k;
                while (tmpMarkedK.Parent is Keyword parent) {
                  tmpMarkedK = parent;
                  if (!parent.Id.Equals(miKeyword.Id)) continue;
                  mi.Keywords.Remove(miKeyword);
                  miKeyword.MediaItems.Remove(mi);
                }
              }
            }
            
            if (mi.Keywords == null)
              mi.Keywords = new List<Keyword>();
            mi.Keywords.Add(k);
            k.MediaItems.Add(mi);

            break;
          }
          case Rating r: {
            mi.Rating = r.Value;
            break;
          }
          case GeoName g: {
            mi.GeoName?.MediaItems.Remove(mi);
            mi.GeoName = g;
            g.MediaItems.Add(mi);
            break;
          }
        }

        mi.SetInfoBox();
      }
    }

    private void ClearItBeforeLoad() {
      Current = null;
      foreach (var item in LoadedItems) {
        SetSelected(item, false);
        item.InfoBoxThumb = null;
        item.InfoBoxPeople = null;
        item.InfoBoxKeywords = null;
      }

      LoadedItems.Clear();
      FilteredItems.Clear();

      foreach (var splittedItem in SplittedItems)
        splittedItem.Clear();

      SplittedItems.Clear();
    }

    public void ReapplyFilter() {
      Current = null;
      ScrollToTop();
      FilteredItems.Clear();

      foreach (var mi in Filter(LoadedItems))
        FilteredItems.Add(mi);

      OnPropertyChanged(nameof(PositionSlashCount));
      App.Core.MarkUsedKeywordsAndPeople();

      SplittedItemsReload();
    }

    private static IEnumerable<MediaItem> Filter(List<MediaItem> mediaItems) {
      //Ratings
      var chosenRatings = App.Core.Ratings.Items.Where(x => x.BackgroundBrush == BackgroundBrush.OrThis).Cast<Rating>().ToArray();
      if (chosenRatings.Any())
        mediaItems = mediaItems.Where(mi => mi.IsNew || chosenRatings.Any(x => x.Value.Equals(mi.Rating))).ToList();

      //MediaItemSizes
      if (!App.Core.MediaItemSizes.Size.AllSizes())
        mediaItems = mediaItems.Where(mi => mi.IsNew || App.Core.MediaItemSizes.Size.Fits(mi.Width * mi.Height)).ToList();

      //People
      var orPeople = App.Core.ActiveFilterItems.OfType<Person>().Where(x => x.BackgroundBrush == BackgroundBrush.OrThis).ToArray();
      var andPeople = App.Core.ActiveFilterItems.OfType<Person>().Where(x => x.BackgroundBrush == BackgroundBrush.AndThis).ToArray();
      var notPeople = App.Core.ActiveFilterItems.OfType<Person>().Where(x => x.BackgroundBrush == BackgroundBrush.Hidden).ToArray();
      var andPeopleAny = andPeople.Any();
      var orPeopleAny = orPeople.Any();
      if (orPeopleAny || andPeopleAny || notPeople.Any()) {
        mediaItems = mediaItems.Where(mi => {
          if (mi.IsNew)
            return true;
          if (mi.People != null && notPeople.Any(p => mi.People.Any(x => x == p)))
            return false;
          if (!andPeopleAny && !orPeopleAny)
            return true;
          if (mi.People != null && andPeopleAny && andPeople.All(p => mi.People.Any(x => x == p)))
            return true;
          if (mi.People != null && orPeople.Any(p => mi.People.Any(x => x == p)))
            return true;

          return false;
        }).ToList();
      }

      //Keywords
      var orKeywords = App.Core.ActiveFilterItems.OfType<Keyword>().Where(x => x.BackgroundBrush == BackgroundBrush.OrThis).ToArray();
      var andKeywords = App.Core.ActiveFilterItems.OfType<Keyword>().Where(x => x.BackgroundBrush == BackgroundBrush.AndThis).ToArray();
      var notKeywords = App.Core.ActiveFilterItems.OfType<Keyword>().Where(x => x.BackgroundBrush == BackgroundBrush.Hidden).ToArray();
      var andKeywordsAny = andKeywords.Any();
      var orKeywordsAny = orKeywords.Any();
      if (orKeywordsAny || andKeywordsAny || notKeywords.Any()) {
        mediaItems = mediaItems.Where(mi => {
          if (mi.IsNew)
            return true;
          if (mi.Keywords != null && notKeywords.Any(k => mi.Keywords.Any(mik => mik.FullPath.StartsWith(k.FullPath))))
            return false;
          if (!andKeywordsAny && !orKeywordsAny)
            return true;
          if (mi.Keywords != null && andKeywordsAny && andKeywords.All(k => mi.Keywords.Any(mik => mik.FullPath.StartsWith(k.FullPath))))
            return true;
          if (mi.Keywords != null && orKeywords.Any(k => mi.Keywords.Any(mik => mik.FullPath.StartsWith(k.FullPath))))
            return true;
          return false;
        }).ToList();
      }

      return mediaItems;
    }

    private async Task<List<MediaItem>> GetMediaItemsFromFoldersAsync(IReadOnlyCollection<Folder> folders, CancellationToken token) {
      var output = new List<MediaItem>();

      await Task.Run(() => {
        foreach (var folder in folders.Where(x => x.IsHidden == false)) {
          if (token.IsCancellationRequested) break;
          var folderMediaItems = new List<MediaItem>();

          // add MediaItems from current Folder to dictionary for faster search
          var fmis = new Dictionary<string, MediaItem>();
          folder.MediaItems.ForEach(mi => fmis.Add(mi.FileName, mi));

          foreach (var file in Directory.EnumerateFiles(folder.FullPath, "*.*", SearchOption.TopDirectoryOnly)) {
            if (token.IsCancellationRequested) break;
            if (Imaging.IsSupportedFileType(file) == false) continue;

            // check if the MediaItem is already in DB, if not put it there
            var fileName = Path.GetFileName(file) ?? string.Empty;
            fmis.TryGetValue(fileName, out var inDbFile);
            if (inDbFile == null) {
              inDbFile = new MediaItem(Helper.GetNextId(), folder, fileName, true);
              AddRecord(inDbFile);
              folder.MediaItems.Add(inDbFile);
            }
            folderMediaItems.Add(inDbFile);
          }

          if (token.IsCancellationRequested) break;

          output.AddRange(folderMediaItems);

          // remove MediaItems deleted outside of this application
          foreach (var fmi in folder.MediaItems.ToArray()) {
            if (folderMediaItems.Contains(fmi)) continue;
            Delete(fmi);
          }
        }
      });

      return output;
    }

    private static async Task<List<MediaItem>> VerifyAccessibilityOfMediaItemsAsync(IReadOnlyCollection<MediaItem> items, CancellationToken token) {
      var output = new List<MediaItem>();

      await Task.Run(() => {
        var folders = (from mi in items select mi.Folder).Distinct();
        var foldersSet = new HashSet<int>();

        foreach (var folder in folders) {
          if (token.IsCancellationRequested) break;
          if (App.Core.CanViewerSeeContentOfThisFolder(folder) == false) continue;
          if (Directory.Exists(folder.FullPath) == false) continue;
          foldersSet.Add(folder.Id);
        }

        foreach (var mi in items) {
          if (token.IsCancellationRequested) break;
          if (foldersSet.Contains(mi.Folder.Id) == false) continue;
          if (File.Exists(mi.FilePath) == false) continue;
          output.Add(mi);
        }
      });

      return output;
    }

    public async Task LoadAsync(List<MediaItem> mediaItems, List<Folder> folders, bool sorted = true) {
      // cancel previous work
      if (_loadCts != null) {
        _loadCts.Cancel();
        await _loadTask;
      }

      // scroll to top to fix funny loading
      if (FilteredItems.Count > 0)
        ScrollTo(FilteredItems[0]);

      ClearItBeforeLoad();
      App.Core.AppInfo.ProgressBarIsIndeterminate = true;

      _loadTask = Task.Run(async () => {
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;
        var items = new List<MediaItem>();

        if (mediaItems != null) {
          // filter out items if directory or file not exists or Viewer can not see items
          items = await VerifyAccessibilityOfMediaItemsAsync(mediaItems, token);
        }

        if (folders != null) {
          items = await GetMediaItemsFromFoldersAsync(folders, token);
        }

        if (sorted)
          items = items.OrderBy(x => x.FileName).ToList();

        // set thumb size and add Media Items to LoadedItems
        foreach (var mi in items) {
          mi.SetThumbSize();
          LoadedItems.Add(mi);
        }

        // filter Media Items and add them to FilteredItems
        foreach (var mi in Filter(LoadedItems)) {
          FilteredItems.Add(mi);
        }

        OnPropertyChanged(nameof(PositionSlashCount));
        App.Core.SetMediaItemSizesLoadedRange();
        await LoadThumbnailsAsync(FilteredItems.ToArray(), token);
      });

      await _loadTask;
    }

    private async Task LoadThumbnailsAsync(IReadOnlyCollection<MediaItem> items, CancellationToken token) {
      App.Core.AppInfo.ProgressBarIsIndeterminate = false;
      App.Core.AppInfo.ProgressBarValueA = 0;
      App.Core.AppInfo.ProgressBarValueB = 0;

      await Task.Run(async () => {
        // read metadata for new items and add thumbnails to grid
        var metadata = ReadMetadataAndListThumbsAsync(items, token);
        // create thumbnails
        var thumbs = Imaging.CreateThumbnailsAsync(items, token);

        await Task.WhenAll(metadata, thumbs);

        var saveDb = metadata.Result;

        if (token.IsCancellationRequested) {
          saveDb = true;
          if (Application.Current.Dispatcher != null)
            await Application.Current.Dispatcher.InvokeAsync(delegate {
              Delete(All.Where(x => x.IsNew).ToArray());
            });
        }

        if (saveDb)
          App.Core.Sdb.SaveAllTables();
      });

      if (Current != null) {
        SetSelected(Current, false);
        SetSelected(Current, true);
      }

      App.Core.MarkUsedKeywordsAndPeople();
      GC.Collect();
    }

    private Task<bool> ReadMetadataAndListThumbsAsync(IReadOnlyCollection<MediaItem> items, CancellationToken token) {
      return Task.Run(() => {
        var mediaItemsModified = false;
        var count = items.Count;
        var workingOn = 0;

        foreach (var mi in items) {
          if (token.IsCancellationRequested) break;

          workingOn++;
          var percent = Convert.ToInt32((double)workingOn / count * 100);

          if (mi.IsNew) {
            mi.IsNew = false;

            Application.Current.Dispatcher?.Invoke(delegate { App.Core.AppInfo.MediaItemsCount++; });

            if (!mi.ReadMetadata()) {
              // delete corrupted MediaItems
              Application.Current.Dispatcher?.Invoke(delegate {
                LoadedItems.Remove(mi);
                FilteredItems.Remove(mi);
                Delete(mi);
                App.Core.AppInfo.ProgressBarValueA = percent;
              });

              continue;
            }

            mi.SetThumbSize();
            mediaItemsModified = true;
          }

          Application.Current.Dispatcher?.Invoke(delegate {
            mi.SetInfoBox();
            SplittedItemsAdd(mi);
            App.Core.AppInfo.ProgressBarValueA = percent;
          });
        }

        return mediaItemsModified;
      });
    }

    public void ScrollToCurrent() {
      if (Current == null)
        ScrollToTop();
      else
        ScrollTo(Current);
    }

    public void ScrollToTop() {
      App.WMain.ThumbsBox.FindChild<ScrollViewer>("ThumbsBoxScrollViewer").ScrollToTop();
    }

    public void ScrollTo(MediaItem mi) {
      var rowIndex = 0;
      foreach (var row in SplittedItems) {
        if (row.Any(x => x.Id.Equals(mi.Id)))
          break;
        rowIndex++;
      }

      App.WMain.ThumbsBox.FindChild<VirtualizingStackPanel>("ThumbsBoxStackPanel").BringIndexIntoViewPublic(rowIndex);
    }

    public void RemoveSelected(bool delete) {
      var items = FilteredItems.Where(x => x.IsSelected).ToList();
      if (items.Count == 0) return;

      // set Current to next MediaItem after last selected or null
      var indexOfNewCurrent = FilteredItems.IndexOf(items[items.Count - 1]) + 1;
      Current = FilteredItems.Count > indexOfNewCurrent ? FilteredItems[indexOfNewCurrent] : null;

      var files = new List<string>();
      var cache = new List<string>();

      foreach (var mi in items) {
        LoadedItems.Remove(mi);
        FilteredItems.Remove(mi);
        if (delete) {
          files.Add(mi.FilePath);
          cache.Add(mi.FilePathCache);
          Delete(mi);
        }
        else SetSelected(mi, false);
      }

      if (delete) {
        AppCore.FileOperationDelete(files, true, false);
        cache.ForEach(File.Delete);
      }

      SplittedItemsReload();

      // set Current to last MediaItem if is null
      if (Current == null && FilteredItems.Count > 0) {
        Current = FilteredItems[FilteredItems.Count - 1];
      }

      ScrollToCurrent();
    }

    private void SplittedItemsAdd(MediaItem mi) {
      var lastRowIndex = SplittedItems.Count - 1;
      if (lastRowIndex == -1) {
        SplittedItems.Add(new ObservableCollection<MediaItem>());
        lastRowIndex++;
      }

      var rowMaxWidth = App.WMain.ThumbsBox.ActualWidth;
      const int itemOffset = 6; //border, margin, padding, ... //TODO find the real value

      var rowWidth = SplittedItems[lastRowIndex].Sum(x => x.ThumbWidth + itemOffset);
      if (mi.ThumbWidth <= rowMaxWidth - rowWidth) {
        SplittedItems[lastRowIndex].Add(mi);
      }
      else {
        SplittedItems.Add(new ObservableCollection<MediaItem>());
        SplittedItems[lastRowIndex + 1].Add(mi);
      }
    }

    public void SplittedItemsReload() {
      foreach (var itemsRow in SplittedItems)
        itemsRow.Clear();

      SplittedItems.Clear();
      App.WMain.UpdateLayout();

      var row = new ObservableCollection<MediaItem>();
      var rowWidth = 0;
      var rowMaxWidth = App.WMain.ThumbsBox.ActualWidth;

      const int itemOffset = 6; //border, margin, padding, ...

      foreach (var mi in FilteredItems) {
        if (mi.ThumbWidth + itemOffset <= rowMaxWidth - rowWidth) {
          row.Add(mi);
          rowWidth += mi.ThumbWidth + itemOffset;
        }
        else {
          SplittedItems.Add(row);
          row = new ObservableCollection<MediaItem> { mi };
          rowWidth = mi.ThumbWidth + itemOffset;
        }
      }

      SplittedItems.Add(row);
    }

    public void ResetThumbsSize() {
      foreach (var item in LoadedItems)
        item.SetThumbSize();
    }

    public void SetOrientation(MediaItem[] mediaItems, Rotation rotation) {
      Helper.IsModified = true;

      var progress = new ProgressBarDialog(App.WMain, true, Environment.ProcessorCount, "Changing orientation ...");
      progress.AddEvents(
        mediaItems,
        null,
        // action
        async delegate(MediaItem mi) {
          var newOrientation = 0;
          switch ((MediaOrientation)mi.Orientation) {
            case MediaOrientation.Rotate90: newOrientation = 90; break;
            case MediaOrientation.Rotate180: newOrientation = 180; break;
            case MediaOrientation.Rotate270: newOrientation = 270; break;
          }

          switch (rotation) {
            case Rotation.Rotate90: newOrientation += 90; break;
            case Rotation.Rotate180: newOrientation += 180; break;
            case Rotation.Rotate270: newOrientation += 270; break;
          }

          if (newOrientation >= 360) newOrientation -= 360;

          switch (newOrientation) {
            case 0: mi.Orientation = (int) MediaOrientation.Normal; break;
            case 90: mi.Orientation = (int) MediaOrientation.Rotate90; break;
            case 180: mi.Orientation = (int) MediaOrientation.Rotate180; break;
            case 270: mi.Orientation = (int) MediaOrientation.Rotate270; break;
          }

          mi.TryWriteMetadata();
          mi.SetThumbSize();
          await Imaging.CreateThumbnailAsync(mi.MediaType, mi.FilePath, mi.FilePathCache, mi.ThumbSize);
          mi.ReloadThumbnail();
        },
        mi => mi.FilePath,
        // onCompleted
        delegate {
          SplittedItemsReload();
          ScrollToCurrent();
          App.Core.Sdb.SaveAllTables();
        });

      progress.StartDialog();
    }

    /// <summary>
    /// Copy or Move MediaItems (Files, Cache and DB)
    /// </summary>
    /// <param name="mode"></param>
    /// <param name="items"></param>
    /// <param name="destFolder"></param>
    public void CopyMove(FileOperationMode mode, List<MediaItem> items, Folder destFolder) {
      var fop = new FileOperationDialog {Owner = App.WMain};

      fop.Worker.DoWork += delegate (object sender, DoWorkEventArgs e) {
        var worker = (BackgroundWorker)sender;
        var mis = (List<MediaItem>)e.Argument;
        var count = mis.Count;
        var done = 0;

        foreach (var mi in mis) {
          if (worker.CancellationPending) {
            e.Cancel = true;
            break;
          }

          worker.ReportProgress(Convert.ToInt32(((double)done / count) * 100),
            new[] { mi.Folder.FullPath, destFolder.FullPath, mi.FileName });

          var miNewFileName = mi.FileName;

          // Open FileOperationCollisionDialog if file with the same name exists in destination
          var destFilePath = Extensions.PathCombine(destFolder.FullPath, mi.FileName);
          if (File.Exists(destFilePath)) {
            var result = AppCore.ShowFileOperationCollisionDialog(mi.FilePath, destFilePath, fop, ref miNewFileName);

            if (result == FileOperationCollisionDialog.CollisionResult.Skip) {
              Application.Current.Dispatcher?.Invoke(delegate {
                App.Core.MediaItems.SetSelected(mi, false);
              });
              continue;
            }
          }

          try {
            switch (mode) {
              case FileOperationMode.Copy: {
                // create object copy
                var miCopy = mi.CopyTo(destFolder, miNewFileName);
                // copy MediaItem and cache on file system
                Directory.CreateDirectory(Path.GetDirectoryName(miCopy.FilePathCache) ?? throw new ArgumentNullException());
                File.Copy(mi.FilePath, miCopy.FilePath, true);
                File.Copy(mi.FilePathCache, miCopy.FilePathCache, true);
                break;
              }
              case FileOperationMode.Move: {
                var srcFilePath = mi.FilePath;
                var srcFilePathCache = mi.FilePathCache;

                // DB
                mi.MoveTo(destFolder, miNewFileName);

                // File System
                if (File.Exists(mi.FilePath))
                  File.Delete(mi.FilePath);
                File.Move(srcFilePath, mi.FilePath);

                // Cache
                if (File.Exists(mi.FilePathCache))
                  File.Delete(mi.FilePathCache);
                Directory.CreateDirectory(Path.GetDirectoryName(mi.FilePathCache) ?? throw new ArgumentNullException());
                File.Move(srcFilePathCache, mi.FilePathCache);
                break;
              }
            }
          }
          catch (Exception ex) {
            ErrorDialog.Show(ex);
          }

          done++;
        }
      };

      fop.PbProgress.IsIndeterminate = false;
      fop.PbProgress.Value = 0;
      fop.Worker.RunWorkerAsync(items);
      fop.ShowDialog();

      if (mode == FileOperationMode.Move) {
        RemoveSelected(false);
        Current = null;
      }
    }

    public MediaItem GetNext() {
      if (Current == null || _indexOfCurrent == null || FilteredItems.Count <= _indexOfCurrent + 1) return null;

      return FilteredItems[(int) _indexOfCurrent + 1];
    }

    public MediaItem GetPrevious() {
      if (Current == null || _indexOfCurrent == null || _indexOfCurrent < 1) return null;

      return FilteredItems[(int) _indexOfCurrent - 1];
    }

    public void Select(bool isCtrlOn, bool isShiftOn, MediaItem mi) {
      if (!isCtrlOn && !isShiftOn) {
        DeselectAll();
        Current = mi;
      }
      else {
        if (isCtrlOn)
          SetSelected(mi, !mi.IsSelected);

        if (isShiftOn && Current != null && _indexOfCurrent != null) {
          var from = (int) _indexOfCurrent;
          var indexOfMi = FilteredItems.IndexOf(mi);
          var to = indexOfMi;
          if (from > to) {
            to = from;
            from = indexOfMi;
          }

          for (var i = from; i < to + 1; i++) {
            SetSelected(FilteredItems[i], true);
          }
        }

        if (Selected == 0)
          Current = null;
        else if (Selected > 1) {
          var current = Current;
          var currentSelected = current?.IsSelected ?? false;
          Current = null;
          if (currentSelected)
            SetSelected(current, true);
        }
      }
    }
  }
}
