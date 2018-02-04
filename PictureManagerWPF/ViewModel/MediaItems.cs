using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using PictureManager.Properties;
using Directory = System.IO.Directory;

namespace PictureManager.ViewModel {
  public class MediaItemsLoad {
    public string FilePath;
    public string FileName;
    public int? DirId;
    public string DirPath;
    public FolderKeyword FolderKeyword;
    public BaseMediaItem MediaItem;
  }

  public class MediaItems {
    public ObservableCollection<BaseMediaItem> Items;
    public List<BaseMediaItem> AllItems;
    public BaseMediaItem Current;
    public AppCore ACore;
    public string[] SuportedExts = { ".jpg", ".jpeg", ".mp4", ".mkv" };
    public string[] SuportedImageExts = { ".jpg", ".jpeg" };
    public string[] SuportedVideoExts = { ".mp4", ".mkv" };

    public MediaItems() {
      ACore = (AppCore) Application.Current.Properties[nameof(AppProps.AppCore)];
      Items = new ObservableCollection<BaseMediaItem>();
      AllItems = new List<BaseMediaItem>();
    }

    public List<BaseMediaItem> GetSelectedOrAll() {
      var mediaItems = Items.Where(x => x.IsSelected).ToList();
      if (mediaItems.Count == 0)
        mediaItems = Items.ToList();
      return mediaItems;
    }

    public void DeselectAll() {
      foreach (var mi in Items.Where(x => x.IsSelected)) {
        mi.IsSelected = false;
      }
      Current = null;
    }

    public void SelectAll() {
      foreach (var mi in Items) {
        mi.IsSelected = true;
      }
    }

    public bool CurrentItemMove(bool next) {
      LoadSiblings();
      if (Current == null) return false;
      var newIndex = next ? Current.Index + 1 : Current.Index - 1;
      if (newIndex < 0 || newIndex >= Items.Count) return false;
      Current.IsSelected = false;
      Current = Items[newIndex];
      Current.IsSelected = true;
      return true;
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
                if (k.FullPath.StartsWith(mi.Keywords[i].FullPath)) {
                  mi.Keywords.RemoveAt(i);
                }
              }
              mi.Keywords.Add(k);
            }
            else 
              mi.Keywords.Remove(k);
            break;
          }
          case Rating r: { mi.Rating = r.Value; break; }
          case GeoName g: { mi.GeoNameId = g.GeoNameId; break; }
        }

        mi.WbUpdateInfo();
      }
    }

    public void Load(BaseTreeViewItem tag, bool recursive) {
      Current = null;
      Items.Clear();

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
        dirs.AddRange(Directory.EnumerateDirectories(topDir, "*", SearchOption.AllDirectories)
          .Select(d => new MediaItemsLoad {DirPath = d}));
      }

      //paring folder with DB
      dirs = (from d in dirs
        join dbd in ACore.Db.Directories on d.DirPath.ToLowerInvariant() equals dbd.Path.ToLowerInvariant() into tmp
        from dbd in tmp.DefaultIfEmpty()
        select new MediaItemsLoad {
          DirPath = d.DirPath,
          DirId = dbd?.Id,
          FolderKeyword = ACore.FolderKeywords.GetFolderKeywordByFullPath(d.DirPath)
        }).ToList();

      //writing new folders to DB and getting all files
      var files = new List<MediaItemsLoad>();
      foreach (var dir in dirs) {
        if (dir.DirId == null) {
          var maxDirId = ACore.Db.GetMaxIdFor<DataModel.Directory>();
          var dirId = ACore.Db.InsertDirecotryInToDb(dir.DirPath);
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
      var chosenRatings = ACore.Ratings.Items.Where(x => x.BgBrush == BgBrushes.OrThis).Cast<Rating>().ToArray();
      if (chosenRatings.Any())
        files = files.Where(f => f.MediaItem == null || chosenRatings.Any(x => x.Value.Equals(f.MediaItem.Rating))).ToList();
      
      //People
      var orPeople = ACore.People.AllPeople.Where(x => x.BgBrush == BgBrushes.OrThis).ToArray();
      var andPeople = ACore.People.AllPeople.Where(x => x.BgBrush == BgBrushes.AndThis).ToArray();
      var notPeople = ACore.People.AllPeople.Where(x => x.BgBrush == BgBrushes.Hidden).ToArray();
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
      var orKeywords = ACore.Keywords.AllKeywords.Where(x => x.BgBrush == BgBrushes.OrThis).ToArray();
      var andKeywords = ACore.Keywords.AllKeywords.Where(x => x.BgBrush == BgBrushes.AndThis).ToArray();
      var notKeywords = ACore.Keywords.AllKeywords.Where(x => x.BgBrush == BgBrushes.Hidden).ToArray();
      var andKeywordsAny = andKeywords.Any();
      var orKeywordsAny = orKeywords.Any();
      if (orKeywordsAny || andKeywordsAny || notKeywords.Any())
        files = files.Where(f => {
          if (f.MediaItem == null)
            return true;
          if (notKeywords.Any(k => f.MediaItem.Keywords.Any(mik => mik.FullPath.StartsWith(k.FullPath))))
            return false;
          if (!andKeywordsAny && !orKeywordsAny)
            return true;
          if (andKeywordsAny && andKeywords.All(k => f.MediaItem.Keywords.Any(mik => mik.FullPath.StartsWith(k.FullPath))))
            return true;
          if (orKeywords.Any(k => f.MediaItem.Keywords.Any(mik => mik.FullPath.StartsWith(k.FullPath))))
            return true;
          return false;
        }).ToList();
      #endregion

      var i = 0;
      foreach (var file in files.OrderBy(f => f.FileName)) {
        if (file.MediaItem == null) {
          var bmi = new BaseMediaItem(file.FilePath, i, null) {FolderKeyword = file.FolderKeyword};
          Items.Add(bmi);
          AllItems.Add(bmi);
        }
        else {
          file.MediaItem.Index = i;
          Items.Add(file.MediaItem);
        }
        i++;
      }

      ACore.UpdateStatusBarInfo();
    }

    public void LoadByTag(BaseTreeViewItem tag, bool recursive) {
      if (tag == null) return;
      Current = null;
      Items.Clear();

      DataModel.MediaItem[] items = null;

      switch (tag) {
        case Keyword keyword: {
            if (recursive) {
              items = (from k in ACore.Db.Keywords.Where(x => x.Name.StartsWith(keyword.FullPath))
                       join mik in ACore.Db.MediaItemKeywords on k.Id equals mik.KeywordId into keywords
                       from k2 in keywords
                       join mi in ACore.Db.MediaItems on k2.MediaItemId equals mi.Id
                       select mi).ToList().Distinct().ToArray();
            } else {
              items = (from mi in ACore.Db.MediaItems
                       join mik in ACore.Db.MediaItemKeywords.Where(x => x.KeywordId == keyword.Id) on mi.Id equals mik.MediaItemId
                       select mi).ToArray();
            }
            break;
          }
        case Person person: {
            items = (from mi in ACore.Db.MediaItems
                     join mip in ACore.Db.MediaItemPeople.Where(x => x.PersonId == person.Id) on mi.Id equals mip.MediaItemId
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
            items = ACore.Db.MediaItems.Where(x => geoNames.Select(gn => (int?) gn.GeoNameId).Contains(x.GeoNameId)).ToArray();
          }
          else {
            items = ACore.Db.MediaItems.Where(x => x.GeoNameId == geoName.GeoNameId).ToArray();
          }
          break;
        }
        case SqlQuery sqlQuery: {
          var miids = (from DataRow dataRow in ACore.Db.Select(sqlQuery.Query) select (long) dataRow["Id"]).ToList();
          items = ACore.Db.MediaItems.Where(x => miids.Contains(x.Id)).ToArray();
          break;
        }
      }

      if (items != null) {
        //Filter by Rating
        var chosenRatings = ACore.Ratings.Items.Where(x => x.BgBrush == BgBrushes.OrThis).Cast<Rating>().ToArray();
        if (chosenRatings.Any())
          items = items.Where(i => chosenRatings.Any(x => x.Value.Equals(i.Rating))).ToArray();
        //Filter by People
        var chosenPeople = ACore.People.AllPeople.Where(x => x.BgBrush == BgBrushes.OrThis).ToArray();
        if (chosenPeople.Any())
          items = items.Where(mi => chosenPeople.Any(
            p => ACore.Db.MediaItemPeople.Exists(mip => mip.MediaItemId == mi.Id && mip.PersonId == p.Id))).ToArray();

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

          var bmi = new BaseMediaItem(filePath, Items.Count, item);
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
    }

    public void ScrollToCurrent() {
      if (Current == null || Current.Index == 0) return;
      ScrollTo(Current.Index);
    }

    public void ScrollTo(int index) {
      ACore.WbThumbs.Document?.GetElementById(index.ToString())?.ScrollIntoView(true);
    }

    public void LoadSiblings() {
      if (!ACore.OneFileOnly) return;
      var filePath = Items[0].FilePath;

      Load(new Folder {FullPath = Path.GetDirectoryName(filePath)}, false);
      Current = Items.FirstOrDefault(x => x.FilePath.Equals(filePath));

      var mi = Items.FirstOrDefault(p => p.FilePath == filePath);
      if (mi != null) mi.IsSelected = true;
      ACore.OneFileOnly = false;
    }

    public void RemoveSelectedFromWeb() {
      var doc = ACore.WbThumbs.Document;
      if (doc == null) return;
      var items = Items.Where(x => x.IsSelected).ToList();
      if (items.Count == 0) return;
      var firstIndex = items[0].Index;

      foreach (var mi in items) {
        var thumb = doc.GetElementById(mi.Index.ToString());
        if (thumb == null) continue;
        thumb.OuterHtml = string.Empty;
        Items.Remove(mi);
      }

      //update index
      var index = 0;
      foreach (var mi in Items) {
        var thumb = doc.GetElementById(mi.Index.ToString());
        thumb?.SetAttribute("id", index.ToString());
        mi.Index = index;
        index++;
      }

      index = firstIndex == 0 ? 0 : firstIndex - 1;
      ScrollTo(index);
    }

    public void SetCurrent() {
      var mis = Items.Where(x => x.IsSelected).ToList();
      Current = mis.Count == 1 ? mis[0] : null;
    }

    public string GetFullScreenInfo() {
      return $"<div>{Items.IndexOf(Current) + 1}/{Items.Count}</div><div>{Current?.Width}x{Current?.Height}</div>{Current?.GetKeywordsAsString(true)}<div>{Current?.FileNameWithExt}</div>";
    }
  }
}
