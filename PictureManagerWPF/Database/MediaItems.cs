using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using Directory = System.IO.Directory;
using PictureManager.ViewModel;

namespace PictureManager.Database {
  public class MediaItems : INotifyPropertyChanged, ITable {
    public TableHelper Helper { get; set; }
    public List<BaseMediaItem> All { get; } = new List<BaseMediaItem>();

    private BaseMediaItem _current;
    private bool _isEditModeOn;

    public ObservableCollection<BaseMediaItem> Items { get; set; } = new ObservableCollection<BaseMediaItem>();
    public ObservableCollection<ObservableCollection<BaseMediaItem>> SplitedItems { get; set; } = new ObservableCollection<ObservableCollection<BaseMediaItem>>();

    public BaseMediaItem Current {
      get => _current;
      set {
        if (_current != null) _current.IsSelected = false;
        _current = value;
        if (_current != null) _current.IsSelected = true;
        OnPropertyChanged();
      }
    }

    public AppCore ACore => (AppCore)Application.Current.Properties[nameof(AppProperty.AppCore)];
    public static string[] SuportedExts = { ".jpg", ".jpeg", ".mp4", ".mkv" };
    public static string[] SuportedImageExts = { ".jpg", ".jpeg" };
    public static string[] SuportedVideoExts = { ".mp4", ".mkv" };

    public bool IsEditModeOn { get => _isEditModeOn; set { _isEditModeOn = value; OnPropertyChanged(); } }

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
      AddRecord(new BaseMediaItem(id, null, props[2]) {
        Csv = props,
        Width = props[3].IntParseOrDefault(0),
        Height = props[4].IntParseOrDefault(0),
        Orientation = props[5].IntParseOrDefault(1),
        Rating = props[6].IntParseOrDefault(0),
        Comment = props[7]
      });
    }

    public void LinkReferences() {
      foreach (var bmi in All) {
        // reference to Folder and back reference from Folder to MediaItems
        bmi.Folder = ACore.Folders.AllDic[int.Parse(bmi.Csv[1])];
        bmi.Folder.MediaItems.Add(bmi);

        // reference to People and back reference from Person to MediaItems
        if (bmi.Csv[9] != string.Empty)
          foreach (var personId in bmi.Csv[9].Split(',')) {
            var p = ACore.People.AllDic[int.Parse(personId)];
            p.MediaItems.Add(bmi);
            bmi.People.Add(p);
          }

        // reference to Keywords and back reference from Keyword to MediaItems
        if (bmi.Csv[10] != string.Empty)
          foreach (var keywordId in bmi.Csv[10].Split(',')) {
            var k = ACore.Keywords.AllDic[int.Parse(keywordId)];
            k.MediaItems.Add(bmi);
            bmi.Keywords.Add(k);
          }

        // reference to GeoName
        if (bmi.Csv[8] != string.Empty) {
          bmi.GeoName = ACore.GeoNames.AllDic[int.Parse(bmi.Csv[8])];
          bmi.GeoName.MediaItems.Add(bmi);
        }

        // csv array is not needed any more
        bmi.Csv = null;
      }
    }

    public void SaveToFile() {
      Helper.SaveToFile(All);
    }

    public void LoadFromFile() {
      All.Clear();
      Helper.LoadFromFile();
    }

    public void AddRecord(BaseMediaItem record) {
      All.Add(record);
    }

    public void Delete(BaseMediaItem item) {
      if (item == null) return;
        
      // remove People
      foreach (var person in item.People)
        person.MediaItems.Remove(item);
      item.People.Clear();

      // remove Keywords
      foreach (var keyword in item.Keywords)
        keyword.MediaItems.Remove(item);
      item.Keywords.Clear();

      // remove item from Folder
      item.Folder.MediaItems.Remove(item);
      item.Folder = null;

      // remove GeoName
      item.GeoName?.MediaItems.Remove(item);
      item.GeoName = null;

      // remove from DB
      All.Remove(item);

      // set MediaItems table as modifed
      Helper.IsModifed = true;
    }

    public List<BaseMediaItem> GetSelectedOrAll() {
      var mediaItems = Items.Where(x => x.IsSelected).ToList();
      return mediaItems.Count == 0 ? Items.ToList() : mediaItems;
    }

    public void SelectAll() {
      foreach (var mi in Items)
        mi.IsSelected = true;
    }

    public void SelectNotModifed() {
      DeselectAll();
      foreach (var mi in Items.Where(x => !x.IsModifed))
        mi.IsSelected = true;
    }

    public void DeselectAll() {
      foreach (var mi in Items.Where(x => x.IsSelected))
        mi.IsSelected = false;

      Current = null;
    }

    public void EditMetadata(object item) {
      foreach (var mi in Items.Where(x => x.IsSelected)) {
        mi.IsModifed = true;

        switch (item) {
          case Person p: {
            if (p.IsMarked) mi.People.Add(p);
            else mi.People.Remove(p);
            break;
          }
          case Keyword k: {
            if (!k.IsMarked) {
              mi.Keywords.Remove(k);
              break;
            }

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

            // remove potencial redundant keywords 
            // example: if marked keyword is "Weather/Sunny" keyword "Weather" is redundant
            foreach (var miKeyword in mi.Keywords.ToArray()) {
              var tmpMarkedK = k;
              while (tmpMarkedK.Parent is Keyword parent) {
                tmpMarkedK = parent;
                if (!parent.Id.Equals(miKeyword.Id)) continue;
                mi.Keywords.Remove(miKeyword);
              }
            }

            mi.Keywords.Add(k);

            break;
          }
          case Rating r: {
            mi.Rating = r.Value;
            break;
          }
          case GeoName g: {
            mi.GeoName = g;
            break;
          }
        }

        mi.SetInfoBox();
      }
    }

    private void ClearItBeforeLoad() {
      Current = null;
      foreach (var item in Items) {
        if (item.IsSelected) item.IsSelected = false;
        item.InfoBoxThumb.Clear();
        item.InfoBoxPeople.Clear();
        item.InfoBoxKeywords.Clear();
      }

      Items.Clear();
      foreach (var splitedItem in SplitedItems) {
        splitedItem.Clear();
      }

      SplitedItems.Clear();
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
      var mediaItems = new List<BaseMediaItem>();
      foreach (var folder in allFolders.Cast<Folder>()) {

        // add MediaItems from current Folder to dictionary for faster search
        var fmis = new Dictionary<string, BaseMediaItem>();
        folder.MediaItems.ForEach(mi => fmis.Add(mi.FileName, mi));

        foreach (var file in Directory.EnumerateFiles(folder.FullPath, "*.*", SearchOption.TopDirectoryOnly)) {
          if (!IsSupportedFileType(file) || !ACore.CanViewerSeeThisFile(file)) continue;

          // check if the MediaItem is already in DB, if not put it there
          var fileName = Path.GetFileName(file) ?? string.Empty;
          fmis.TryGetValue(fileName, out var inDbFile);
          if (inDbFile == null) {
            inDbFile = new BaseMediaItem(Helper.GetNextId(), folder, fileName, true);
            AddRecord(inDbFile);
            folder.MediaItems.Add(inDbFile);
          }
          mediaItems.Add(inDbFile);
        }
      }

      #region Filtering

      //Ratings
      var chosenRatings = ACore.Ratings.Items.Where(x => x.BackgroundBrush == BackgroundBrush.OrThis).Cast<Rating>().ToArray();
      if (chosenRatings.Any())
        mediaItems = mediaItems.Where(mi => mi.IsNew || chosenRatings.Any(x => x.Value.Equals(mi.Rating))).ToList();

      //MediaItemSizes
      if (!ACore.MediaItemSizes.Size.AllSizes())
        mediaItems = mediaItems.Where(mi => mi.IsNew || ACore.MediaItemSizes.Size.Fits(mi.Width * mi.Height)).ToList();

      //People
      var orPeople = ACore.People.All.Where(x => x.BackgroundBrush == BackgroundBrush.OrThis).ToArray();
      var andPeople = ACore.People.All.Where(x => x.BackgroundBrush == BackgroundBrush.AndThis).ToArray();
      var notPeople = ACore.People.All.Where(x => x.BackgroundBrush == BackgroundBrush.Hidden).ToArray();
      var andPeopleAny = andPeople.Any();
      var orPeopleAny = orPeople.Any();
      if (orPeopleAny || andPeopleAny || notPeople.Any()) {
        mediaItems = mediaItems.Where(mi => {
          if (mi.IsNew)
            return true;
          if (notPeople.Any(p => mi.People.Any(x => x == p)))
            return false;
          if (!andPeopleAny && !orPeopleAny)
            return true;
          if (andPeopleAny && andPeople.All(p => mi.People.Any(x => x == p)))
            return true;
          if (orPeople.Any(p => mi.People.Any(x => x == p)))
            return true;

          return false;
        }).ToList();
      }

      //Keywords
      var orKeywords = ACore.Keywords.All.Where(x => x.BackgroundBrush == BackgroundBrush.OrThis).ToArray();
      var andKeywords = ACore.Keywords.All.Where(x => x.BackgroundBrush == BackgroundBrush.AndThis).ToArray();
      var notKeywords = ACore.Keywords.All.Where(x => x.BackgroundBrush == BackgroundBrush.Hidden).ToArray();
      var andKeywordsAny = andKeywords.Any();
      var orKeywordsAny = orKeywords.Any();
      if (orKeywordsAny || andKeywordsAny || notKeywords.Any()) {
        mediaItems = mediaItems.Where(mi => {
          if (mi.IsNew)
            return true;
          if (notKeywords.Any(k => mi.Keywords.Any(mik => mik.FullPath.StartsWith(k.FullPath))))
            return false;
          if (!andKeywordsAny && !orKeywordsAny)
            return true;
          if (andKeywordsAny && andKeywords.All(k => mi.Keywords.Any(mik => mik.FullPath.StartsWith(k.FullPath))))
            return true;
          if (orKeywords.Any(k => mi.Keywords.Any(mik => mik.FullPath.StartsWith(k.FullPath))))
            return true;
          return false;
        }).ToList();
      }

      #endregion

      var i = 0;
      foreach (var mi in mediaItems.OrderBy(x => x.FileName)) {
        mi.SetThumbSize();
        mi.Index = i++;
        Items.Add(mi);
      }

      ACore.SetMediaItemSizesLoadedRange();
      ACore.UpdateStatusBarInfo();
    }

    public void LoadByTag(BaseTreeViewItem tag, bool recursive) {
      ClearItBeforeLoad();

      // get items by tag
      BaseMediaItem[] items = null;

      switch (tag) {
        case Keyword keyword: items = keyword.GetMediaItems(recursive); break;
        case Person person: items = person.MediaItems.ToArray(); break;
        case GeoName geoName: items = geoName.GetMediaItems(recursive); break;
      }

      if (items == null) return;

      // filter out items if directory or file not exists or Viewer can not see items
      ACore.AppInfo.ProgressBarIsIndeterminate = true;

      if (_loadByTagWorker == null) {
        _loadByTagWorker = new BackgroundWorker {WorkerSupportsCancellation = true};

        _loadByTagWorker.DoWork += delegate(object sender, DoWorkEventArgs e) {
          var worker = (BackgroundWorker) sender;
          if (worker.CancellationPending) {
            e.Cancel = true;
            return;
          }

          var allItems = (BaseMediaItem[]) e.Argument;
          var resultItems = new List<BaseMediaItem>();
          var dirs = (from mi in allItems select mi.Folder).Distinct()
            .Where(dir => Directory.Exists(dir.FullPath)).ToDictionary(dir => dir.Id);

          var i = -1;
          foreach (var item in allItems.OrderBy(x => x.FileName)) {
            if (!dirs.ContainsKey(item.Folder.Id)) continue;
            if (!File.Exists(item.FilePath)) continue;

            // Filter by Viewer
            if (!ACore.CanViewerSeeThisFile(item.FilePath)) continue;

            item.Index = ++i;
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

          foreach (var item in (List<BaseMediaItem>) e.Result)
            Items.Add(item);

          ACore.SetMediaItemSizesLoadedRange();
          ACore.UpdateStatusBarInfo();
          ScrollTo(0);
          ACore.LoadThumbnails();
        };
      }

      if (_loadByTagWorker.IsBusy) {
        _loadByTagWorker.CancelAsync();
        return;
      }

      _loadByTagWorker.RunWorkerAsync(items);
    }

    public void ScrollToCurrent() {
      if (Current == null) return;
      ScrollTo(Current.Index);
    }

    public void ScrollTo2(int index) {
      var count = 0;
      var rowIndex = 0;

      foreach (var row in SplitedItems) {
        count += row.Count;
        if (count < index) {
          rowIndex++;
          continue;
        }

        break;
      }

      var itemContainer = AppCore.WMain.ThumbsBox.ItemContainerGenerator.ContainerFromIndex(rowIndex) as ContentPresenter;
      itemContainer?.BringIntoView();
    }

    public void ScrollTo(int index) {
      var scroll = AppCore.WMain.ThumbsBox.FindChild<ScrollViewer>("ThumbsBoxScrollViewer");
      if (index == 0) {
        scroll.ScrollToTop();
        return;
      }

      var count = 0;
      var rowsHeight = 0;
      const int itemOffset = 5; //BorderThickness, Margin 
      foreach (var row in SplitedItems) {
        count += row.Count;
        if (count < index) {
          rowsHeight += row.Max(x => x.ThumbHeight) + itemOffset;
          continue;
        }

        break;
      }

      scroll.ScrollToVerticalOffset(rowsHeight);
      ScrollTo2(index);
    }

    public void RemoveSelected(bool delete) {
      var firstIndex = Items.FirstOrDefault(x => x.IsSelected)?.Index;
      if (firstIndex == null) return;

      var files = new List<string>();
      var cache = new List<string>();

      foreach (var item in Items.Where(x => x.IsSelected).ToList()) {
        Items.Remove(item);
        if (delete) {
          files.Add(item.FilePath);
          cache.Add(item.FilePathCache);
          Delete(item);
        }
        else item.IsSelected = false;
      }

      if (delete) {
        ACore.FileOperationDelete(files, true, false);
        cache.ForEach(File.Delete);
      }

      //update index
      var index = 0;
      foreach (var mi in Items) {
        mi.Index = index;
        index++;
      }

      SplitedItemsReload();
      Current = null;
      var count = Items.Count;
      if (count > 0) {
        if (count == firstIndex) firstIndex--;
        Current = Items[(int)firstIndex];
        ScrollToCurrent();
      }
    }

    public void SplitedItemsAdd(BaseMediaItem bmi) {
      var lastIndex = SplitedItems.Count - 1;
      if (lastIndex == -1) {
        SplitedItems.Add(new ObservableCollection<BaseMediaItem>());
        lastIndex++;
      }

      var rowMaxWidth = AppCore.WMain.ThumbsBox.ActualWidth;
      const int itemOffset = 6; //border, margin, padding, ... //TODO find the real value

      var rowWidth = SplitedItems[lastIndex].Sum(x => x.ThumbWidth + itemOffset);
      if (bmi.ThumbWidth <= rowMaxWidth - rowWidth) {
        SplitedItems[lastIndex].Add(bmi);
      }
      else {
        SplitedItems.Add(new ObservableCollection<BaseMediaItem>());
        SplitedItems[lastIndex + 1].Add(bmi);
      }
    }

    public void SplitedItemsReload() {
      foreach (var itemsRow in SplitedItems)
        itemsRow.Clear();

      SplitedItems.Clear();

      AppCore.WMain.UpdateLayout();
      var rowMaxWidth = AppCore.WMain.ActualWidth - AppCore.WMain.GridMain.ColumnDefinitions[0].ActualWidth - 3 -
                        SystemParameters.VerticalScrollBarWidth;
      var rowWidth = 0;
      const int itemOffset = 6; //border, margin, padding, ...
      var row = new ObservableCollection<BaseMediaItem>();
      foreach (var item in Items) {
        if (item.ThumbWidth + itemOffset <= rowMaxWidth - rowWidth) {
          row.Add(item);
          rowWidth += item.ThumbWidth + itemOffset;
        }
        else {
          SplitedItems.Add(row);
          row = new ObservableCollection<BaseMediaItem> { item };
          rowWidth = item.ThumbWidth + itemOffset;
        }
      }

      SplitedItems.Add(row);
    }

    public void ResetThumbsSize() {
      foreach (var item in Items) {
        item.SetThumbSize();
      }
    }

    public static bool IsSupportedFileType(string filePath) {
      return SuportedExts.Any(x => x.Equals(Path.GetExtension(filePath), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Copy or Move MediaItems (Files, Cache and DB)
    /// </summary>
    /// <param name="mode"></param>
    /// <param name="items"></param>
    /// <param name="destFolder"></param>
    public void CopyMove(FileOperationMode mode, List<BaseMediaItem> items, Folder destFolder) {
      var fop = new Dialogs.FileOperationDialog { Owner = AppCore.WMain };

      fop.Worker.DoWork += delegate (object sender, DoWorkEventArgs e) {
        var worker = (BackgroundWorker)sender;
        var mis = (List<BaseMediaItem>)e.Argument;
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
            var result = ACore.ShowFileOperationCollisionDialog(mi.FilePath, destFilePath, fop, ref miNewFileName);

            if (result == Dialogs.FileOperationCollisionDialog.CollisionResult.Skip) {
              mi.IsSelected = false;
              continue;
            }
          }

          try {
            switch (mode) {
              case FileOperationMode.Copy: {
                // create object copy
                var miCopy = mi.CopyTo(destFolder, miNewFileName);
                // copy MediaItem and cache on file system
                Directory.CreateDirectory(Path.GetDirectoryName(miCopy.FilePathCache));
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
                Directory.CreateDirectory(Path.GetDirectoryName(mi.FilePathCache));
                File.Move(srcFilePathCache, mi.FilePathCache);
                break;
              }
            }
          }
          catch (Exception ex) {
            AppCore.ShowErrorDialog(ex);
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
        ACore.UpdateStatusBarInfo();
      }
    }

  }
}
