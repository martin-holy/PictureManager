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
  public class MediaItems {
    public ObservableCollection<BaseMediaItem> Items;
    public BaseMediaItem Current;
    public AppCore ACore;
    public string[] SuportedExts = { ".jpg", ".jpeg" };

    public MediaItems() {
      ACore = (AppCore) Application.Current.Properties[nameof(AppProps.AppCore)];
      Items = new ObservableCollection<BaseMediaItem>();
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
      foreach (BaseMediaItem mi in Items.Where(x => x.IsSelected)) {
        mi.IsModifed = true;

        switch (item.GetType().Name) {
          case nameof(Person): {
            var person = (Person) item;
            if (person.IsMarked) mi.People.Add(person); else mi.People.Remove(person);
            break;
          }
          case nameof(Keyword): {
            var keyword = (Keyword) item;
            if (keyword.IsMarked) {
              //remove potencial redundant keywords (example: if new keyword is "#CoSpi/Sunny" keyword "#CoSpi" is redundant)
              for (int i = mi.Keywords.Count - 1; i >= 0; i--) {
                if (keyword.FullPath.StartsWith(mi.Keywords[i].FullPath)) {
                  mi.Keywords.RemoveAt(i);
                }
              }
              mi.Keywords.Add(keyword);
            } else {
              mi.Keywords.Remove(keyword);
            }
            break;
          }
          case nameof(Rating): {
            var rating = (Rating) item;
            mi.Rating = rating.Value;
            break;
          }
          case nameof(GeoName): {
            mi.GeoNameId = ((GeoName) item).GeoNameId;
            break;
          }
        }
        mi.WbUpdateInfo();
      }
    }

    public void LoadByFolder(string path, bool recursive) {
      if (!Directory.Exists(path)) return;
      Current = null;
      Items.Clear();

      var dirs = new Dictionary<string, object[]> {{path, new object[] {null, null}}};
      if (recursive) {
        foreach (var d in Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories)) {
          dirs.Add(d, new object[] {null, null});
        }
      }

      foreach (var d in dirs) {
        //TODO predelat tak aby se nemuselo davat FK.Load
        var maxDirId = ACore.Db.GetMaxIdFor<DataModel.Directory>();
        var dirId = ACore.Db.InsertDirecotryInToDb(d.Key);
        if (dirId > maxDirId) ACore.FolderKeywords.Load();
        d.Value[0] = dirId;
        d.Value[1] = ACore.FolderKeywords.GetFolderKeywordByFullPath(d.Key);
      }

      var dbItems = new List<DataModel.MediaItem>();
      var chosenRatings = ACore.Ratings.Items.Where(x => x.BgBrush == BgBrushes.Chosen).Cast<Rating>().ToArray();
      var chosenPeople = ACore.People.AllPeople.Where(x => x.BgBrush == BgBrushes.Chosen).Cast<Person>().ToArray();

      foreach (var file in Directory.EnumerateFiles(path, "*.*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
        .Where(f => SuportedExts.Any(x => f.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
        .OrderBy(Path.GetFileName)) {

        var filePath = file.Replace(":\\\\", ":\\");

        //Filter by Viewer
        if (!ACore.CanViewerSeeThisFile(filePath)) continue;

        var dir = dirs.SingleOrDefault(x => x.Key == Path.GetDirectoryName(file));
        var item = ACore.Db.MediaItems.SingleOrDefault(x => x.DirectoryId == (int) dir.Value[0] &&
                                                            x.FileName.Equals(Path.GetFileName(file)));
        if (item != null) {
          //Filter by Rating
          if (chosenRatings.Any() && !chosenRatings.Any(x => x.Value.Equals(item.Rating))) continue;
          //Filter by People
          if (chosenPeople.Any() && ACore.Db.MediaItemPeople.Count(
            mip => mip.MediaItemId == item.Id && chosenPeople.Any(p => p.Id == mip.PersonId)) == 0) continue;

          dbItems.Add(item);
        }

        var pic = new Picture(filePath, Items.Count, item) {
          DirId = (int) dir.Value[0],
          FolderKeyword = (FolderKeyword) dir.Value[1]
        };
        Items.Add(pic);
      }

      //Load People and Keywords for thous that are already in DB
      var mips = (from mip in ACore.Db.MediaItemPeople
                  join mi in dbItems on mip.MediaItemId equals mi.Id
                  select mip).ToArray();

      var miks = (from mik in ACore.Db.MediaItemKeywords
                  join mi in dbItems on mik.MediaItemId equals mi.Id
                  select mik).ToArray();

      foreach (var item in Items.Where(x => x.Data != null)) {
        //Load People
        foreach (var mip in mips.Where(x => x.MediaItemId == item.Id)) {
          item.People.Add(ACore.People.GetPerson(mip.PersonId));
        }
        //Load Keywords
        foreach (var mik in miks.Where(x => x.MediaItemId == item.Id)) {
          item.Keywords.Add(ACore.Keywords.GetKeyword(mik.KeywordId));
        }
      }

      ACore.UpdateStatusBarInfo();
    }

    public void LoadByTag(BaseTreeViewTagItem tag, bool recursive) {
      if (tag == null) return;
      Current = null;
      Items.Clear();

      DataModel.MediaItem[] items = null;

      switch (tag.GetType().Name) {
        case nameof(Keyword): {
            var keyword = (Keyword)tag;
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
        case nameof(Person): {
            var person = (Person)tag;
            items = (from mi in ACore.Db.MediaItems
                     join mip in ACore.Db.MediaItemPeople.Where(x => x.PersonId == person.Id) on mi.Id equals mip.MediaItemId
                     select mi).ToArray();
            break;
          }
        case nameof(FolderKeyword): {
            var folderKeyword = (FolderKeyword)tag;
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
        case nameof(GeoName): {
          var geoName = (GeoName) tag;
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
        case nameof(SqlQuery): {
          var sqlQuery = (SqlQuery) tag;
          var miids = (from DataRow dataRow in ACore.Db.Select(sqlQuery.Query) select (long) dataRow["Id"]).ToList();
          items = ACore.Db.MediaItems.Where(x => miids.Contains(x.Id)).ToArray();
          break;
        }
      }

      if (items != null) {
        //Filter by Rating
        var chosenRatings = ACore.Ratings.Items.Where(x => x.BgBrush == BgBrushes.Chosen).Cast<Rating>().ToArray();
        if (chosenRatings.Any())
          items = items.Where(i => chosenRatings.Any(x => x.Value.Equals(i.Rating))).ToArray();
        //Filter by People
        var chosenPeople = ACore.People.AllPeople.Where(x => x.BgBrush == BgBrushes.Chosen).ToArray();
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

          Picture pic = new Picture(filePath, Items.Count, item);
          //Load People
          foreach (var mip in mips.Where(x => x.MediaItemId == item.Id)) {
            pic.People.Add(ACore.People.GetPerson(mip.PersonId));
          }
          //Load Keywords
          foreach (var mik in miks.Where(x => x.MediaItemId == item.Id)) {
            pic.Keywords.Add(ACore.Keywords.GetKeyword(mik.KeywordId));
          }
          //Folder Keyword
          pic.FolderKeyword = ACore.FolderKeywords.GetFolderKeywordByFullPath(Path.GetDirectoryName(filePath));

          //TODO docasne, pak odstranit
          var viewer = ACore.Viewers.Items.Cast<Viewer>().SingleOrDefault(x => x.Title == Settings.Default.Viewer);
          if (viewer != null && viewer.Title.Equals("Prezentace")) {
            if (pic.Keywords.Any(x => x.FullPath.StartsWith("#CoSpi/Weed"))) continue;
          }

          Items.Add(pic);
        }
      }

      ACore.UpdateStatusBarInfo();
    }

    public void LoadByFilter(Filter filter) {
      Current = null;
      Items.Clear();

      //TODO zjistit jak číst filter.FilterData :D

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

      LoadByFolder(Path.GetDirectoryName(filePath), false);
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
