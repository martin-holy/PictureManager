using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls;
using Directory = System.IO.Directory;
using VM = PictureManager.ViewModel;

namespace PictureManager.Database {
  public class MediaItems : INotifyPropertyChanged, ITable {
    public Dictionary<int, IRecord> Records { get; set; } = new Dictionary<int, IRecord>();

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

    public bool IsEditModeOn {
      get => _isEditModeOn;
      set {
        _isEditModeOn = value;
        OnPropertyChanged();
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public void NewFromCsv(string csv) {
      // ID|Folder|Name|Width|Height|Orientation|Rating|Comment|GeoName|People|Keywords
      var props = csv.Split('|');
      if (props.Length != 11) return;
      var id = int.Parse(props[0]);
      Records.Add(id, new BaseMediaItem(id, null, props[2]) {
        Csv = props,
        Width = props[3].IntParseOrDefault(0),
        Height = props[4].IntParseOrDefault(0),
        Orientation = props[5].IntParseOrDefault(1),
        Rating = props[6].IntParseOrDefault(0),
        Comment = props[7]
      });
    }

    public void LinkReferences(SimpleDB sdb) {
      foreach (var item in Records) {
        var bmi = (BaseMediaItem)item.Value;

        // reference to Folder
        bmi.Folder = (Folder) ACore.NewFolders.Records[int.Parse(bmi.Csv[1])];

        // reference to GeoName
        if (bmi.Csv[8] != string.Empty) {
          bmi.GeoName = (GeoName) ACore.NewGeoNames.Records[int.Parse(bmi.Csv[8])];
          bmi.GeoName.MediaItems.Add(bmi);
        }
          

        // reference to People and back reference from Person to MediaItems
        if (bmi.Csv[9] != string.Empty)
          foreach (var personId in bmi.Csv[9].Split(',')) {
            var p = (Person) ACore.NewPeople.Records[int.Parse(personId)];
            p.MediaItems.Add(bmi);
            bmi.People.Add(p);
          }

        // reference to Keywords and back reference from Keyword to MediaItems
        if (bmi.Csv[10] != string.Empty)
          foreach (var keywordId in bmi.Csv[9].Split(',')) {
            var k = (Keyword) ACore.NewKeywords.Records[int.Parse(keywordId)];
            k.MediaItems.Add(bmi);
            bmi.Keywords.Add(k);
          }

        // csv array is not needed any more
        bmi.Csv = null;
      }
    }

    public void ReLoad(List<BaseMediaItem> items) {
      //TODO
      /*items.ForEach(mi => {
        ACore.Db.ReloadItem(mi.Data);
        mi.IsModifed = false;
      });

      LoadPeople(items);
      LoadKeywords(items);

      items.ForEach(mi => mi.SetInfoBox());*/
    }

    public List<BaseMediaItem> GetSelectedOrAll() {
      var mediaItems = Items.Where(x => x.IsSelected).ToList();
      return mediaItems.Count == 0 ? Items.ToList() : mediaItems;
    }

    public void SelectAll() {
      foreach (var mi in Items) {
        mi.IsSelected = true;
      }
    }

    public void SelectNotModifed() {
      DeselectAll();
      foreach (var mi in Items.Where(x => !x.IsModifed)) {
        mi.IsSelected = true;
      }
    }

    public void DeselectAll() {
      foreach (var mi in Items.Where(x => x.IsSelected)) {
        mi.IsSelected = false;
      }

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
              if (k.IsMarked) {
                //TODO DEBUG this
                //remove potencial redundant keywords (example: if new keyword is "#CoSpi/Sunny" keyword "#CoSpi" is redundant)
                for (var i = mi.Keywords.Count - 1; i >= 0; i--) {
                  if (k.GetFullPath().StartsWith(mi.Keywords[i].Title)) {
                    mi.Keywords.RemoveAt(i);
                  }
                }

                mi.Keywords.Add(k);
              }
              else
                mi.Keywords.Remove(k);

              break;
            }
          case ViewModel.Rating r: {
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

    private void ClearBeforeLoad() {
      Current = null;
      foreach (var item in Items) {
        if (item.IsSelected) item.IsSelected = false;
      }

      Items.Clear();
      foreach (var splitedItem in SplitedItems) {
        splitedItem.Clear();
      }

      SplitedItems.Clear();
    }

    public void Load(VM.BaseTreeViewItem tag, bool recursive) {
      ClearBeforeLoad();

      var topDirs = new List<string>();
      switch (tag) {
        case Folder f: {
            topDirs.Add(f.FullPath);
            break;
          }
        case FolderKeyword fk: {
            topDirs.AddRange(ACore.Db.Directories.Where(d => fk.FolderIdList.Contains(d.Id)).Select(d => d.Path));
            break;
          }
      }

      //getting all folders
      var dirs = new List<MediaItemsLoad>();
      foreach (var topDir in topDirs) {
        dirs.Add(new MediaItemsLoad { DirPath = topDir });
        if (!recursive) continue;
        dirs.AddRange(AppCore.GetAllDirectoriesSafely(topDir)
          .Select(d => new MediaItemsLoad { DirPath = d }));
      }

      //pairing folder with DB
      dirs = (from d in dirs
              join dbd in ACore.Db.Directories on d.DirPath.ToLowerInvariant() equals dbd.Path.ToLowerInvariant() into tmp
              from dbd in tmp.DefaultIfEmpty()
              select new MediaItemsLoad {
                DirPath = d.DirPath,
                DirId = dbd?.Id ?? -1,
                FolderKeyword = ACore.FolderKeywords.GetFolderKeywordByFullPath(d.DirPath)
              }).ToList();

      //writing new folders to DB and getting all files
      var files = new List<MediaItemsLoad>();
      foreach (var dir in dirs) {
        if (dir.DirId == -1) {
          var maxDirId = ACore.Db.GetMaxIdFor<DataModel.Directory>();
          var dirId = ACore.Db.InsertDirectoryInToDb(dir.DirPath);
          if (dirId > maxDirId) ACore.FolderKeywords.Load();
          dir.DirId = dirId;
          dir.FolderKeyword = ACore.FolderKeywords.GetFolderKeywordByFullPath(dir.DirPath);
        }

        files.AddRange(from file in Directory.EnumerateFiles(dir.DirPath, "*.*", SearchOption.TopDirectoryOnly)
                       where IsSupportedFileType(file) && ACore.CanViewerSeeThisFile(file)
                       select new MediaItemsLoad {
                         FilePath = file,
                         FileName = Path.GetFileName(file),
                         DirPath = dir.DirPath,
                         DirId = dir.DirId,
                         FolderKeyword = dir.FolderKeyword
                       });
      }

      //pairing files with DB
      files = (from f in files
               join mi in ACore.MediaItems.AllItems on
                 new { file = f.FileName.ToLowerInvariant(), dir = f.DirId } equals
                 new { file = mi.Data.FileName.ToLowerInvariant(), dir = mi.Data.DirectoryId } into tmp
               from mi in tmp.DefaultIfEmpty()
               select new MediaItemsLoad {
                 FilePath = f.FilePath,
                 FileName = f.FileName,
                 DirId = f.DirId,
                 DirPath = f.DirPath,
                 FolderKeyword = f.FolderKeyword,
                 MediaItem = mi
               }).ToList();

      files.ForEach(x => {
        if (x.MediaItem != null)
          x.MediaItem.FolderKeyword = x.FolderKeyword;
      });

      #region Filtering

      //Ratings
      var chosenRatings = ACore.Ratings.Items.Where(x => x.BackgroundBrush == BackgroundBrush.OrThis).Cast<Rating>()
        .ToArray();
      if (chosenRatings.Any())
        files = files.Where(f => f.MediaItem == null || chosenRatings.Any(x => x.Value.Equals(f.MediaItem.Data.Rating)))
          .ToList();

      //MediaItemSizes
      if (!ACore.MediaItemSizes.Size.AllSizes()) {
        files = files.Where(f =>
            f.MediaItem == null || ACore.MediaItemSizes.Size.Fits(f.MediaItem.Data.Width * f.MediaItem.Data.Height))
          .ToList();
      }

      //People
      var orPeople = ACore.People.AllPeople.Where(x => x.BackgroundBrush == BackgroundBrush.OrThis).ToArray();
      var andPeople = ACore.People.AllPeople.Where(x => x.BackgroundBrush == BackgroundBrush.AndThis).ToArray();
      var notPeople = ACore.People.AllPeople.Where(x => x.BackgroundBrush == BackgroundBrush.Hidden).ToArray();
      var andPeopleAny = andPeople.Any();
      var orPeopleAny = orPeople.Any();
      if (orPeopleAny || andPeopleAny || notPeople.Any())
        files = files.Where(f => {
          if (f.MediaItem == null)
            return true;
          if (notPeople.Any(p => f.MediaItem.People.Any(x => x == p)))
            return false;
          if (!andPeopleAny && !orPeopleAny)
            return true;
          if (andPeopleAny && andPeople.All(p => f.MediaItem.People.Any(x => x == p)))
            return true;
          if (orPeople.Any(p => f.MediaItem.People.Any(x => x == p)))
            return true;

          return false;
        }).ToList();

      //Keywords
      var orKeywords = ACore.Keywords.AllKeywords.Where(x => x.BackgroundBrush == BackgroundBrush.OrThis).ToArray();
      var andKeywords = ACore.Keywords.AllKeywords.Where(x => x.BackgroundBrush == BackgroundBrush.AndThis).ToArray();
      var notKeywords = ACore.Keywords.AllKeywords.Where(x => x.BackgroundBrush == BackgroundBrush.Hidden).ToArray();
      var andKeywordsAny = andKeywords.Any();
      var orKeywordsAny = orKeywords.Any();
      if (orKeywordsAny || andKeywordsAny || notKeywords.Any())
        files = files.Where(f => {
          if (f.MediaItem == null)
            return true;
          if (notKeywords.Any(k => f.MediaItem.Keywords.Any(mik => mik.Data.Name.StartsWith(k.Data.Name))))
            return false;
          if (!andKeywordsAny && !orKeywordsAny)
            return true;
          if (andKeywordsAny &&
              andKeywords.All(k => f.MediaItem.Keywords.Any(mik => mik.Data.Name.StartsWith(k.Data.Name))))
            return true;
          if (orKeywords.Any(k => f.MediaItem.Keywords.Any(mik => mik.Data.Name.StartsWith(k.Data.Name))))
            return true;
          return false;
        }).ToList();

      #endregion

      var i = 0;
      foreach (var file in files.OrderBy(f => f.FileName)) {
        if (file.MediaItem == null) {
          var bmi = new BaseMediaItem(file.FilePath,
            new DataModel.MediaItem {
              Id = ACore.Db.GetNextIdFor<DataModel.MediaItem>(),
              FileName = file.FileName,
              DirectoryId = file.DirId
            }, true) {
            FolderKeyword = file.FolderKeyword,
            Index = i
          };
          Items.Add(bmi);
          AllItems.Add(bmi);
        }
        else {
          file.MediaItem.Index = i;
          file.MediaItem.SetThumbSize();
          Items.Add(file.MediaItem);
        }

        i++;
      }

      ACore.SetMediaItemSizesLoadedRange();
      ACore.UpdateStatusBarInfo();
    }

    public void LoadByTag(VM.BaseTreeViewItem tag, bool recursive) {
      ClearBeforeLoad();

      BaseMediaItem[] items = null;

      switch (tag) {
        case Keyword keyword: items = keyword.GetMediaItems(recursive); break;
        case Person person: items = person.MediaItems.ToArray(); break;
        case GeoName geoName: items = geoName.GetMediaItems(recursive); break;
      }

      if (items == null) return;

      var dirs = (from mi in items select mi.Folder).Distinct()
        .Where(dir => Directory.Exists(dir.FullPath)).ToDictionary(dir => dir.Id);

      var i = -1;
      foreach (var item in items.OrderBy(x => x.FileName)) {
        if (!dirs.ContainsKey(item.Folder.Id)) continue;
        if (!File.Exists(item.FilePath)) continue;

        //Filter by Viewer
        if (!ACore.CanViewerSeeThisFile(item.FilePath)) continue;

        item.Index = ++i;
        item.SetThumbSize();
        Items.Add(item);
      }

      ACore.SetMediaItemSizesLoadedRange();
      ACore.UpdateStatusBarInfo();
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
      foreach (var item in Items.ToList()) {
        if (!item.IsSelected) continue;
        Items.Remove(item);
        if (delete) Records.Remove(item.Id);
        else item.IsSelected = false;
      }

      //update index
      var index = 0;
      foreach (var mi in Items) {
        mi.Index = index;
        index++;
      }

      SplitedItemsReload();
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
      foreach (var itemsRow in SplitedItems) {
        itemsRow.Clear();
      }

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
      if (SuportedImageExts.Any(x => filePath.EndsWith(x, StringComparison.OrdinalIgnoreCase))) {
        // chceck if is image valid
        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
          try {
            BitmapDecoder.Create(fs, BitmapCreateOptions.None, BitmapCacheOption.None);
            return true;
          }
          catch (Exception) {
            return false;
          }
        }
      }

      if (SuportedVideoExts.Any(x => filePath.EndsWith(x, StringComparison.OrdinalIgnoreCase))) {
        return true;
      }

      return false;
    }
  }

  public class MediaItemsLoad {
    public string FilePath;
    public string FileName;
    public int DirId;
    public string DirPath;
    public FolderKeyword FolderKeyword;
    public BaseMediaItem MediaItem;
  }
}
