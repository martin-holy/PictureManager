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

namespace PictureManager.ViewModel {
  public class MediaItemsLoad {
    public string FilePath;
    public string FileName;
    public int DirId;
    public string DirPath;
    public FolderKeyword FolderKeyword;
    public BaseMediaItem MediaItem;
  }

  public class MediaItems: INotifyPropertyChanged {
    private BaseMediaItem _current;
    private bool _isEditModeOn;

    public ObservableCollection<BaseMediaItem> Items { get; set; } = new ObservableCollection<BaseMediaItem>();
    public List<BaseMediaItem> AllItems = new List<BaseMediaItem>();
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

    public AppCore ACore => (AppCore) Application.Current.Properties[nameof(AppProperty.AppCore)];
    public string[] SuportedExts = { ".jpg", ".jpeg", ".mp4", ".mkv" };
    public string[] SuportedImageExts = { ".jpg", ".jpeg" };
    public string[] SuportedVideoExts = { ".mp4", ".mkv" };
    public bool IsEditModeOn { get => _isEditModeOn; set { _isEditModeOn = value; OnPropertyChanged(); } }

    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public void LoadAllItems() {
      AllItems.Clear();
      AllItems.AddRange(
        ACore.Db.MediaItems.Join(ACore.Db.Directories, mi => mi.DirectoryId, dir => dir.Id,
          (mi, dir) => new BaseMediaItem(Path.Combine(dir.Path, mi.FileName), mi)));
    }

    public void LoadPeople(List<BaseMediaItem> items) {
      ACore.Db.MediaItemPeople.Join(
          ACore.People.AllPeople, mip => mip.PersonId, p => p.Data.Id, (mip, p) => new {mip.MediaItemId, p})
        .Join(items, mip => mip.MediaItemId, mi => mi.Data.Id, (mip, mi) => new {mi, mip.p})
        .ToList().ForEach(x => x.mi.People.Add(x.p));
    }

    public void LoadKeywords(List<BaseMediaItem> items) {
      ACore.Db.MediaItemKeywords.Join(
          ACore.Keywords.AllKeywords, mik => mik.KeywordId, k => k.Data.Id, (mik, k) => new {mik.MediaItemId, k})
        .Join(items, mik => mik.MediaItemId, mi => mi.Data.Id, (mik, mi) => new {mi, mik.k})
        .ToList().ForEach(x => x.mi.Keywords.Add(x.k));
    }

    public void ReLoad(List<BaseMediaItem> items) {
      items.ForEach(mi => {
        ACore.Db.ReloadItem(mi.Data);
        mi.IsModifed = false;
      });

      LoadPeople(items);
      LoadKeywords(items);

      items.ForEach(mi => mi.SetInfoBox());
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

    public void DeselectAll() {
      foreach (var mi in Items.Where(x => x.IsSelected)) {
        mi.IsSelected = false;
      }
      Current = null;
    }

    public void CurrentItemMove(bool next) {
      Current = Items[next ? Current.Index + 1 : Current.Index - 1];
    }

    public void EditMetadata(object item) {
      foreach (var mi in Items.Where(x => x.IsSelected)) {
        mi.IsModifed = true;

        switch (item) {
          case Person p: { if (p.IsMarked) mi.People.Add(p); else mi.People.Remove(p); break; }
          case Keyword k: {
            if (k.IsMarked) {
              //remove potencial redundant keywords (example: if new keyword is "#CoSpi/Sunny" keyword "#CoSpi" is redundant)
              for (var i = mi.Keywords.Count - 1; i >= 0; i--) {
                if (k.Data.Name.StartsWith(mi.Keywords[i].Data.Name)) {
                  mi.Keywords.RemoveAt(i);
                }
              }
              mi.Keywords.Add(k);
            }
            else 
              mi.Keywords.Remove(k);
            break;
          }
          case Rating r: { mi.Data.Rating = r.Value; break; }
          case GeoName g: { mi.Data.GeoNameId = g.Data.GeoNameId; break; }
        }

        mi.SetInfoBox();
      }
    }

    public void Load(BaseTreeViewItem tag, bool recursive) {
      Current = null;
      Items.Clear();
      foreach (var splitedItem in SplitedItems) { splitedItem.Clear(); }
      SplitedItems.Clear();

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

      //getting all folder
      var dirs = new List<MediaItemsLoad>();
      foreach (var topDir in topDirs) {
        dirs.Add(new MediaItemsLoad {DirPath = topDir});
        if (!recursive) continue;
        dirs.AddRange(AppCore.GetAllDirectoriesSafely(topDir)
          .Select(d => new MediaItemsLoad { DirPath = d }));
      }

      //paring folder with DB
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
            .Where(f => SuportedExts.Any(x => f.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
          where ACore.CanViewerSeeThisFile(file)
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
        f.FilePath.ToLowerInvariant() equals mi.FilePath.ToLowerInvariant() into tmp
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
      var chosenRatings = ACore.Ratings.Items.Where(x => x.BackgroundBrush == BackgroundBrush.OrThis).Cast<Rating>().ToArray();
      if (chosenRatings.Any())
        files = files.Where(f => f.MediaItem == null || chosenRatings.Any(x => x.Value.Equals(f.MediaItem.Data.Rating))).ToList();
      
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
          if (andKeywordsAny && andKeywords.All(k => f.MediaItem.Keywords.Any(mik => mik.Data.Name.StartsWith(k.Data.Name))))
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

      ACore.UpdateStatusBarInfo();
    }

    /*public void LoadByTag(BaseTreeViewItem tag, bool recursive) {
      if (tag == null) return;
      Current = null;
      Items.Clear();

      DataModel.MediaItem[] items = null;

      switch (tag) {
        case Keyword keyword: {
            if (recursive) {
              items = (from k in ACore.Db.Keywords.Where(x => x.Name.StartsWith(keyword.Data.Name))
                       join mik in ACore.Db.MediaItemKeywords on k.Id equals mik.KeywordId into keywords
                       from k2 in keywords
                       join mi in ACore.Db.MediaItems on k2.MediaItemId equals mi.Id
                       select mi).ToList().Distinct().ToArray();
            } else {
              items = (from mi in ACore.Db.MediaItems
                       join mik in ACore.Db.MediaItemKeywords.Where(x => x.KeywordId == keyword.Data.Id) on mi.Id equals mik.MediaItemId
                       select mi).ToArray();
            }
            break;
          }
        case Person person: {
            items = (from mi in ACore.Db.MediaItems
                     join mip in ACore.Db.MediaItemPeople.Where(x => x.PersonId == person.Data.Id) on mi.Id equals mip.MediaItemId
                     select mi).ToArray();
            break;
          }
        case FolderKeyword folderKeyword: {
            if (recursive) {
              var itemss = new List<DataModel.MediaItem>();
              foreach (var fkDir in ACore.Db.Directories.Where(x => folderKeyword.FolderIdList.Contains(x.Id))) {
                foreach (var dir in ACore.Db.Directories.Where(x => x.Path.StartsWith(fkDir.Path))) {
                  foreach (var mi in ACore.Db.MediaItems.Where(x => x.DirectoryId == dir.Id)) {
                    itemss.Add(mi);
                  }
                }
              }
              items = itemss.OrderBy(x => x.FileName).ToArray();
            } else {
              items = ACore.Db.MediaItems.Where(x => folderKeyword.FolderIdList.Contains(x.DirectoryId)).ToArray();
            }
            break;
          }
        case GeoName geoName: {
          if (recursive) {
            var geoNames = new List<GeoName>();
            geoName.GetThisAndSubGeoNames(ref geoNames);
            items = ACore.Db.MediaItems.Where(x => geoNames.Select(gn => (int?) gn.Data.GeoNameId).Contains(x.GeoNameId)).ToArray();
          }
          else {
            items = ACore.Db.MediaItems.Where(x => x.GeoNameId == geoName.Data.GeoNameId).ToArray();
          }
          break;
        }
        case SqlQuery sqlQuery: {
          var miids = (from DataRow dataRow in ACore.Db.Select(sqlQuery.Data.Query) select (long) dataRow["Id"]).ToList();
          items = ACore.Db.MediaItems.Where(x => miids.Contains(x.Id)).ToArray();
          break;
        }
      }

      if (items != null) {
        //Filter by Rating
        var chosenRatings = ACore.Ratings.Items.Where(x => x.BackgroundBrush == BackgroundBrush.OrThis).Cast<Rating>().ToArray();
        if (chosenRatings.Any())
          items = items.Where(i => chosenRatings.Any(x => x.Value.Equals(i.Rating))).ToArray();
        //Filter by People
        var chosenPeople = ACore.People.AllPeople.Where(x => x.BackgroundBrush == BackgroundBrush.OrThis).ToArray();
        if (chosenPeople.Any())
          items = items.Where(mi => chosenPeople.Any(
            p => ACore.Db.MediaItemPeople.Exists(mip => mip.MediaItemId == mi.Id && mip.PersonId == p.Data.Id))).ToArray();

        var allDirs = (from d in ACore.Db.Directories
                       join mi in items on d.Id equals mi.DirectoryId
                       select d).Distinct().ToArray();
        var dirs = allDirs.Where(dir => Directory.Exists(dir.Path)).ToDictionary(dir => dir.Id);

        var mips = (from mip in ACore.Db.MediaItemPeople
                    join mi in items on mip.MediaItemId equals mi.Id
                    select mip).ToArray();

        var miks = (from mik in ACore.Db.MediaItemKeywords
                    join mi in items on mik.MediaItemId equals mi.Id
                    select mik).ToArray();

        foreach (var item in items.OrderBy(x => x.FileName)) {
          if (!dirs.ContainsKey(item.DirectoryId)) continue;
          var filePath = Path.Combine(dirs[item.DirectoryId].Path, item.FileName);
          if (!File.Exists(filePath)) continue;

          //Filter by Viewer
          if (!ACore.CanViewerSeeThisFile(filePath)) continue;
          //zakomentovano protoze stejne predelat
          /*var bmi = new BaseMediaItem(filePath, Items.Count, item);
          //Load People
          foreach (var mip in mips.Where(x => x.MediaItemId == item.Id)) {
            bmi.People.Add(ACore.People.GetPerson(mip.PersonId));
          }
          //Load Keywords
          foreach (var mik in miks.Where(x => x.MediaItemId == item.Id)) {
            bmi.Keywords.Add(ACore.Keywords.GetKeyword(mik.KeywordId));
          }
          //Folder Keyword
          bmi.FolderKeyword = ACore.FolderKeywords.GetFolderKeywordByFullPath(Path.GetDirectoryName(filePath));

          //TODO docasne, pak odstranit
          var viewer = ACore.Viewers.Items.Cast<Viewer>().SingleOrDefault(x => x.Title == Settings.Default.Viewer);
          if (viewer != null && viewer.Title.Equals("Prezentace")) {
            if (bmi.Keywords.Any(x => x.FullPath.StartsWith("#CoSpi/Weed"))) continue;
          }

          Items.Add(bmi);
        }
      }

      ACore.UpdateStatusBarInfo();
    }*/

    public void ScrollToCurrent() {
      if (Current == null || Current.Index == 0) return;
      ScrollTo(Current.Index);
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
    }

    public void RemoveSelected() {
      var firstIndex = Items.FirstOrDefault(x => x.IsSelected)?.Index;
      if (firstIndex == null) return;
      //Items = Items.Where(x => !x.IsSelected).ToList();
      foreach (var item in Items.ToList()) {
        if (item.IsSelected)
          Items.Remove(item);
      }

      //update index
      var index = 0;
      foreach (var mi in Items) {
        mi.Index = index;
        index++;
      }

      SplitedItemsReload();
      ScrollTo(firstIndex == 0 ? 0 : (int) firstIndex - 1);
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

      var rowMaxWidth = AppCore.WMain.ThumbsBox.ActualWidth;
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
          row = new ObservableCollection<BaseMediaItem> {item};
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
  }
}
