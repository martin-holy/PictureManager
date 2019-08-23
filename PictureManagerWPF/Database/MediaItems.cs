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
    public List<MediaItem> All { get; } = new List<MediaItem>();

    private MediaItem _current;
    private bool _isEditModeOn;

    public ObservableCollection<MediaItem> Items { get; } = new ObservableCollection<MediaItem>();
    public ObservableCollection<ObservableCollection<MediaItem>> SplitedItems { get; } = new ObservableCollection<ObservableCollection<MediaItem>>();

    public MediaItem Current {
      get => _current;
      set {
        if (_current != null) _current.IsSelected = false;
        _current = value;
        if (_current != null) _current.IsSelected = true;
        OnPropertyChanged();
      }
    }

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

      // set MediaItems table as modifed
      Helper.IsModifed = true;
    }

    public List<MediaItem> GetSelectedOrAll() {
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

    public void SetMetadata(object tag) {
      foreach (var mi in Items.Where(x => x.IsSelected)) {
        mi.IsModifed = true;

        switch (tag) {
          case Person p: {
            if (p.IsMarked) {
              if (mi.People == null)
                mi.People = new List<Person>();
              mi.People.Add(p);
            }
            else {
              mi.People.Remove(p);
              if (mi.People.Count == 0)
                mi.People = null;
            }
            break;
          }
          case Keyword k: {
            if (!k.IsMarked) {
              mi.Keywords.Remove(k);
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
            }
            
            if (mi.Keywords == null)
              mi.Keywords = new List<Keyword>();
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
        item.InfoBoxThumb = null;
        item.InfoBoxPeople = null;
        item.InfoBoxKeywords = null;
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
      var mediaItems = new List<MediaItem>();
      foreach (var folder in allFolders.Cast<Folder>()) {

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
          mediaItems.Add(inDbFile);
        }
      }

      #region Filtering

      //Ratings
      var chosenRatings = App.Core.Ratings.Items.Where(x => x.BackgroundBrush == BackgroundBrush.OrThis).Cast<Rating>().ToArray();
      if (chosenRatings.Any())
        mediaItems = mediaItems.Where(mi => mi.IsNew || chosenRatings.Any(x => x.Value.Equals(mi.Rating))).ToList();

      //MediaItemSizes
      if (!App.Core.MediaItemSizes.Size.AllSizes())
        mediaItems = mediaItems.Where(mi => mi.IsNew || App.Core.MediaItemSizes.Size.Fits(mi.Width * mi.Height)).ToList();

      //People
      var orPeople = App.Core.People.All.Where(x => x.BackgroundBrush == BackgroundBrush.OrThis).ToArray();
      var andPeople = App.Core.People.All.Where(x => x.BackgroundBrush == BackgroundBrush.AndThis).ToArray();
      var notPeople = App.Core.People.All.Where(x => x.BackgroundBrush == BackgroundBrush.Hidden).ToArray();
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
      var orKeywords = App.Core.Keywords.All.Where(x => x.BackgroundBrush == BackgroundBrush.OrThis).ToArray();
      var andKeywords = App.Core.Keywords.All.Where(x => x.BackgroundBrush == BackgroundBrush.AndThis).ToArray();
      var notKeywords = App.Core.Keywords.All.Where(x => x.BackgroundBrush == BackgroundBrush.Hidden).ToArray();
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

      #endregion

      var i = 0;
      foreach (var mi in mediaItems.OrderBy(x => x.FileName)) {
        mi.SetThumbSize();
        mi.Index = i++;
        Items.Add(mi);
      }

      App.Core.SetMediaItemSizesLoadedRange();
      App.Core.UpdateStatusBarInfo();
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

          var allItems = (MediaItem[]) e.Argument;
          var resultItems = new List<MediaItem>();
          var dirs = (from mi in allItems select mi.Folder).Distinct()
            .Where(dir => Directory.Exists(dir.FullPath)).ToDictionary(dir => dir.Id);

          var i = -1;
          foreach (var item in allItems.OrderBy(x => x.FileName)) {
            if (!dirs.ContainsKey(item.Folder.Id)) continue;
            if (!File.Exists(item.FilePath)) continue;

            // Filter by Viewer
            if (!Viewers.CanViewerSeeThisFile(App.Core.CurrentViewer, item.FilePath)) continue;

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

          foreach (var item in (List<MediaItem>) e.Result)
            Items.Add(item);

          App.Core.SetMediaItemSizesLoadedRange();
          App.Core.UpdateStatusBarInfo();
          ScrollTo(0);
          App.Core.LoadThumbnails();
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

      var itemContainer = App.WMain.ThumbsBox.ItemContainerGenerator.ContainerFromIndex(rowIndex) as ContentPresenter;
      itemContainer?.BringIntoView();
    }

    public void ScrollTo(int index) {
      var scroll = App.WMain.ThumbsBox.FindChild<ScrollViewer>("ThumbsBoxScrollViewer");
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
        AppCore.FileOperationDelete(files, true, false);
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

    public void SplitedItemsAdd(MediaItem mi) {
      var lastIndex = SplitedItems.Count - 1;
      if (lastIndex == -1) {
        SplitedItems.Add(new ObservableCollection<MediaItem>());
        lastIndex++;
      }

      var rowMaxWidth = App.WMain.ThumbsBox.ActualWidth;
      const int itemOffset = 6; //border, margin, padding, ... //TODO find the real value

      var rowWidth = SplitedItems[lastIndex].Sum(x => x.ThumbWidth + itemOffset);
      if (mi.ThumbWidth <= rowMaxWidth - rowWidth) {
        SplitedItems[lastIndex].Add(mi);
      }
      else {
        SplitedItems.Add(new ObservableCollection<MediaItem>());
        SplitedItems[lastIndex + 1].Add(mi);
      }
    }

    public void SplitedItemsReload() {
      foreach (var itemsRow in SplitedItems)
        itemsRow.Clear();

      SplitedItems.Clear();

      App.WMain.UpdateLayout();
      var rowMaxWidth = App.WMain.ActualWidth - App.WMain.GridMain.ColumnDefinitions[0].ActualWidth - 3 -
                        SystemParameters.VerticalScrollBarWidth;
      var rowWidth = 0;
      const int itemOffset = 6; //border, margin, padding, ...
      var row = new ObservableCollection<MediaItem>();
      foreach (var item in Items) {
        if (item.ThumbWidth + itemOffset <= rowMaxWidth - rowWidth) {
          row.Add(item);
          rowWidth += item.ThumbWidth + itemOffset;
        }
        else {
          SplitedItems.Add(row);
          row = new ObservableCollection<MediaItem> { item };
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

    private static readonly HashSet<char> CommentAllowedCharacters = new HashSet<char>("@#$€_&+-()*':;!?=<>% ");

    public static string NormalizeComment(string comment) {
      return string.IsNullOrEmpty(comment)
        ? null
        : new string(comment.Where(x => char.IsLetterOrDigit(x) || CommentAllowedCharacters.Contains(x)).ToArray());
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
    public void CopyMove(FileOperationMode mode, List<MediaItem> items, Folder destFolder) {
      var fop = new Dialogs.FileOperationDialog { Owner = App.WMain };

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
        App.Core.UpdateStatusBarInfo();
      }
    }

  }
}
