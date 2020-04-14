using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls;
using PictureManager.Dialogs;
using PictureManager.Properties;
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

    public static string[] SupportedExts = { ".jpg", ".jpeg", ".mp4", ".mkv" };
    public static string[] SupportedImageExts = { ".jpg", ".jpeg" };
    public static string[] SupportedVideoExts = { ".mp4", ".mkv" };

    public bool IsEditModeOn { get => _isEditModeOn; set { _isEditModeOn = value; OnPropertyChanged(); } }
    public int Selected { get => _selected; set { _selected = value; OnPropertyChanged(); } }
    public string PositionSlashCount => $"{(Current == null ? string.Empty : $"{_indexOfCurrent + 1}/")}{FilteredItems.Count}";

    public int ModifiedCount => ModifiedItems.Count;
    public List<MediaItem> ModifiedItems = new List<MediaItem>();

    private BackgroundWorker _loadByTagWorker;

    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    ~MediaItems() {
      if (_loadByTagWorker == null) return;
      _loadByTagWorker.Dispose();
      _loadByTagWorker = null;
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

    private static List<MediaItem> Filter(List<MediaItem> mediaItems) {
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

    public void Load(BaseTreeViewItem tag, bool recursive) {
      ClearItBeforeLoad();

      // get top folders
      var topFolders = new List<Folder>();
      switch (tag) {
        case Folder f: topFolders.Add(f); break;
        case FolderKeyword fk: topFolders.AddRange(fk.Folders); break;
      }

      // get all folders
      var allFolders = new List<BaseTreeViewItem>();
      foreach (var topFolder in topFolders) {
        if (recursive) {
          topFolder.LoadSubFolders(true);
          topFolder.GetThisAndItemsRecursive(ref allFolders);
        }
        else {
          allFolders.Add(topFolder);
        }
      }

      // get all MediaItems
      var mediaItems = new List<MediaItem>();
      var folderMediaItems = new List<MediaItem>();
      foreach (var folder in allFolders.Cast<Folder>()) {
        folderMediaItems.Clear();

        // add MediaItems from current Folder to dictionary for faster search
        var fmis = new Dictionary<string, MediaItem>();
        folder.MediaItems.ForEach(mi => fmis.Add(mi.FileName, mi));
        
        foreach (var file in Directory.EnumerateFiles(folder.FullPath, "*.*", SearchOption.TopDirectoryOnly)) {
          if (!IsSupportedFileType(file) || !Viewers.CanViewerSeeThisFile(App.Core.CurrentViewer, file)) continue;

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

        mediaItems.AddRange(folderMediaItems);

        // remove MediaItems deleted outside of this application
        foreach (var fmi in folder.MediaItems.ToArray()) {
          if (folderMediaItems.Contains(fmi)) continue;
          Delete(fmi);
        }
      }

      foreach (var mi in mediaItems.OrderBy(x => x.FileName)) {
        mi.SetThumbSize();
        LoadedItems.Add(mi);
      }

      foreach (var mi in Filter(LoadedItems))
        FilteredItems.Add(mi);

      App.Core.SetMediaItemSizesLoadedRange();
      OnPropertyChanged(nameof(PositionSlashCount));
    }

    public void LoadByTag(BaseTreeViewItem tag, bool recursive) {
      ClearItBeforeLoad();

      // get items by tag
      MediaItem[] items = null;

      switch (tag) {
        case Keyword keyword: items = keyword.GetMediaItems(recursive); break;
        case Person person: items = person.MediaItems.ToArray(); break;
        case GeoName geoName: items = geoName.GetMediaItems(recursive); break;
      }

      if (items == null) return;

      // filter out items if directory or file not exists or Viewer can not see items
      App.Core.AppInfo.ProgressBarIsIndeterminate = true;

      if (_loadByTagWorker == null) {
        _loadByTagWorker = new BackgroundWorker {WorkerSupportsCancellation = true};

        _loadByTagWorker.DoWork += delegate(object sender, DoWorkEventArgs e) {
          var worker = (BackgroundWorker) sender;
          if (worker.CancellationPending) {
            e.Cancel = true;
            return;
          }

          var allItems = (MediaItem[]) ((object[]) e.Argument)[0];
          var resultItems = new List<MediaItem>();
          var dirs = (from mi in allItems select mi.Folder).Distinct()
            .Where(dir => Directory.Exists(dir.FullPath)).ToDictionary(dir => dir.Id);

          foreach (var item in allItems.OrderBy(x => x.FileName)) {
            if (!dirs.ContainsKey(item.Folder.Id)) continue;
            if (!File.Exists(item.FilePath)) continue;

            // Filter by Viewer
            if (!Viewers.CanViewerSeeThisFile(App.Core.CurrentViewer, item.FilePath)) continue;

            item.SetThumbSize();
            resultItems.Add(item);
          }

          e.Result = resultItems;
        };

        _loadByTagWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e) {
          if (e.Cancelled) {
            // run with new items
            _loadByTagWorker.RunWorkerAsync(items);
            return;
          }

          // add loaded items
          foreach (var mi in (List<MediaItem>)e.Result)
            LoadedItems.Add(mi);

          // add filtered items
          foreach (var mi in Filter(LoadedItems))
            FilteredItems.Add(mi);

          App.Core.SetMediaItemSizesLoadedRange();
          OnPropertyChanged(nameof(PositionSlashCount));
          ScrollToTop();
          App.Core.LoadThumbnails(App.Core.MediaItems.FilteredItems.ToArray());
        };
      }

      if (_loadByTagWorker.IsBusy) {
        _loadByTagWorker.CancelAsync();
        return;
      }

      _loadByTagWorker.RunWorkerAsync(new object[] {items, tag});
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
      var firstSelected = FilteredItems.FirstOrDefault(x => x.IsSelected);
      if (firstSelected == null) return;
      var indexOfFirstSelected = FilteredItems.IndexOf(firstSelected);

      var files = new List<string>();
      var cache = new List<string>();

      foreach (var mi in FilteredItems.Where(x => x.IsSelected).ToList()) {
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
      Current = null;

      // set new current
      var count = FilteredItems.Count;
      if (count == 0) return;
      if (count == indexOfFirstSelected) indexOfFirstSelected--;
      Current = FilteredItems[indexOfFirstSelected];
      ScrollToCurrent();
    }

    public void SplittedItemsAdd(MediaItem mi) {
      var lastIndex = SplittedItems.Count - 1;
      if (lastIndex == -1) {
        SplittedItems.Add(new ObservableCollection<MediaItem>());
        lastIndex++;
      }

      var rowMaxWidth = App.WMain.ThumbsBox.ActualWidth;
      const int itemOffset = 6; //border, margin, padding, ... //TODO find the real value

      var rowWidth = SplittedItems[lastIndex].Sum(x => x.ThumbWidth + itemOffset);
      if (mi.ThumbWidth <= rowMaxWidth - rowWidth) {
        SplittedItems[lastIndex].Add(mi);
      }
      else {
        SplittedItems.Add(new ObservableCollection<MediaItem>());
        SplittedItems[lastIndex + 1].Add(mi);
      }
    }

    public void SplittedItemsReload() {
      foreach (var itemsRow in SplittedItems)
        itemsRow.Clear();

      SplittedItems.Clear();
      App.WMain.UpdateLayout();

      var row = new ObservableCollection<MediaItem>();
      var rowWidth = 0;
      var rowMaxWidth = App.WMain.ActualWidth - App.WMain.GridMain.ColumnDefinitions[0].ActualWidth - 3 -
                        SystemParameters.VerticalScrollBarWidth;
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

    private static readonly HashSet<char> CommentAllowedCharacters = new HashSet<char>("@#$€_&+-()*':;!?=<>% ");

    public static string NormalizeComment(string comment) {
      return string.IsNullOrEmpty(comment)
        ? null
        : new string(comment.Where(x => char.IsLetterOrDigit(x) || CommentAllowedCharacters.Contains(x)).ToArray());
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
          await App.Core.CreateThumbnailAsync(mi.MediaType, mi.FilePath, mi.FilePathCache, mi.ThumbSize);
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

    public static bool IsSupportedFileType(string filePath) {
      return SupportedExts.Any(x => x.Equals(Path.GetExtension(filePath), StringComparison.OrdinalIgnoreCase));
    }

    public static MediaType GetMediaType(string filePath) {
      return SupportedImageExts.Any(
        x => filePath.EndsWith(x, StringComparison.InvariantCultureIgnoreCase))
        ? MediaType.Image
        : MediaType.Video;
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

    public static void Resize(string src, string dest, int px, bool withMetadata, bool withThumbnail) {
      int GreatestCommonDivisor(int a, int b) {
        while (a != 0 && b != 0) {
          if (a > b)
            a %= b;
          else
            b %= a;
        }

        return a == 0 ? b : a;
      }

      void SetIfContainsQuery(BitmapMetadata bm, string query, object value) {
        if (bm.ContainsQuery(query))
          bm.SetQuery(query, value);
      }

      var srcFile = new FileInfo(src);
      var destFile = new FileInfo(dest);

      using (Stream srcFileStream = File.Open(srcFile.FullName, FileMode.Open, FileAccess.Read)) {
        var decoder = BitmapDecoder.Create(srcFileStream, BitmapCreateOptions.None, BitmapCacheOption.None);
        if (decoder.CodecInfo == null || !decoder.CodecInfo.FileExtensions.Contains("jpg") || decoder.Frames[0] == null) return;

        var firstFrame = decoder.Frames[0];

        var pxw = firstFrame.PixelWidth; // image width
        var pxh = firstFrame.PixelHeight; // image height
        var gcd = GreatestCommonDivisor(pxw, pxh);
        var rw = pxw / gcd; // image ratio
        var rh = pxh / gcd; // image ratio
        var q = Math.Sqrt((double) px / (rw * rh)); // Bulgarian constant
        var stw = (q * rw) / pxw; // scale transform X
        var sth = (q * rh) / pxh; // scale transform Y

        var resized = new TransformedBitmap(firstFrame, new ScaleTransform(stw, sth, 0, 0));
        var metadata = withMetadata ? firstFrame.Metadata?.Clone() as BitmapMetadata : new BitmapMetadata("jpg");
        var thumbnail = withThumbnail ? firstFrame.Thumbnail : null;

        if (!withMetadata) {
          // even when withMetadata == false, set orientation
          var orientation = ((BitmapMetadata) firstFrame.Metadata)?.GetQuery("System.Photo.Orientation") ?? (ushort) 1;
          metadata.SetQuery("System.Photo.Orientation", orientation);
        }

        // ifd ImageWidth a ImageHeight
        SetIfContainsQuery(metadata, "/app1/ifd/{ushort=256}", resized.PixelWidth);
        SetIfContainsQuery(metadata, "/app1/ifd/{ushort=257}", resized.PixelHeight);
        // exif ExifImageWidth a ExifImageHeight
        SetIfContainsQuery(metadata, "/app1/ifd/exif/{ushort=40962}", resized.PixelWidth);
        SetIfContainsQuery(metadata, "/app1/ifd/exif/{ushort=40963}", resized.PixelHeight);

        var encoder = new JpegBitmapEncoder { QualityLevel = Settings.Default.JpegQualityLevel };

        encoder.Frames.Add(BitmapFrame.Create(resized, thumbnail, metadata, firstFrame.ColorContexts));

        using (Stream destFileStream = File.Open(destFile.FullName, FileMode.Create, FileAccess.ReadWrite)) {
          encoder.Save(destFileStream);
        }

        // set LastWriteTime to destination file as DateTaken so it can be correctly sorted in mobile apps
        var date = DateTime.MinValue;
        
        // try to first get dateTaken from file name
        var match = Regex.Match(srcFile.Name, "[0-9]{8}_[0-9]{6}");
        if (match.Success)
          DateTime.TryParseExact(match.Value, "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

        // try to get dateTaken from metadata
        if (date == DateTime.MinValue) {
          var dateTaken = ((BitmapMetadata)firstFrame.Metadata)?.DateTaken;
          DateTime.TryParse(dateTaken, out date);
        }
        
        if (date != DateTime.MinValue)
          destFile.LastWriteTime = date;
      }
    }

    // TODO vyuzit tohle v MediaItem.SetThumbSize
    public static Size GetThumbSize(double width, double height, int desiredSize) {
      var size = new Size();

      if (width > height) {
        //panorama
        if (width / height > 16.0 / 9.0) {
          const int maxWidth = 1100;
          var panoramaHeight = desiredSize / 16.0 * 9;
          var tooBig = panoramaHeight / height * width > maxWidth;
          size.Height = tooBig ? maxWidth / width * height : panoramaHeight;
          size.Width = tooBig ? maxWidth : panoramaHeight / height * width;
          return size;
        }

        size.Height = desiredSize / width * height;
        size.Width = desiredSize;
        return size;
      }

      size.Height = desiredSize;
      size.Width = desiredSize / height * width;
      return size;
    }
  }
}
