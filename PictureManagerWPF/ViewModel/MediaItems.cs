using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PictureManager.ViewModel {
  public class MediaItems {
    public ObservableCollection<BaseMediaItem> Items;
    public BaseMediaItem Current;
    public AppCore ACore;
    public WebBrowser WbThumbs => ACore.WbThumbs;
    public DataModel.PmDataContext Db => ACore.Db;
    public string[] SuportedExts = { ".jpg", ".jpeg" };

    public MediaItems(AppCore aCore) {
      ACore = aCore;
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
        }
        mi.WbUpdateInfo();
      }
    }

    public void LoadByFolder(string path) {
      if (!Directory.Exists(path)) return;

      var dirId = ACore.InsertDirecotryInToDb(path);
      ACore.FolderKeywords.Load();
      FolderKeyword fk = ACore.FolderKeywords.GetFolderKeywordByFullPath(path);

      Current = null;
      Items.Clear();

      foreach (string file in Directory.EnumerateFiles(path)
        .Where(f => SuportedExts.Any(x => f.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
        .OrderBy(x => x)) {
          Items.Add(new Picture(file.Replace(":\\\\", ":\\"), Db, Items.Count, WbThumbs, null) 
            { DirId = dirId, FolderKeyword = fk });
      }

      ACore.UpdateStatusBarInfo();
    }

    public void LoadByTag(BaseTreeViewTagItem tag, bool recursive) {
      Current = null;
      Items.Clear();

      IQueryable<DataModel.MediaItem> items = null;

      switch (tag.GetType().Name) {
        case nameof(Keyword): {
          var keyword = (Keyword) tag;
          if (recursive) {
            items = from k in Db.Keywords.Where(x => x.Name.StartsWith(keyword.FullPath))
              join mik in Db.MediaItemKeywords on k.Id equals mik.KeywordId into keywords
              from k2 in keywords
              join mi in Db.MediaItems on k2.MediaItemId equals mi.Id
              select mi;
          } else {
            items = from mi in Db.MediaItems
              join mik in Db.MediaItemKeywords.Where(x => x.KeywordId == keyword.Id) on mi.Id equals mik.MediaItemId
              select mi;
          }
          break;
        }
        case nameof(Person): {
          var person = (Person) tag;
          items = from mi in Db.MediaItems
            join mip in Db.MediaItemPeople.Where(x => x.PersonId == person.Id) on mi.Id equals mip.MediaItemId
            select mi;
          break;
        }
        case nameof(FolderKeyword): {
          var folderKeyword = (FolderKeyword) tag;
          if (recursive) {
            var itemss = new List<DataModel.MediaItem>();
            foreach (var fkDir in Db.Directories.Where(x => folderKeyword.FolderIdList.Contains(x.Id))) {
              foreach (var dir in Db.Directories.Where(x => x.Path.StartsWith(fkDir.Path))) {
                foreach (var mi in Db.MediaItems.Where(x => x.DirectoryId == dir.Id)) {
                  itemss.Add(mi);
                }
              }
            }
            items = itemss.OrderBy(x => x.FileName).AsQueryable();
          } else {
            items = Db.MediaItems.Where(x => folderKeyword.FolderIdList.Contains(x.DirectoryId));
          }
          break;
        }
      }

      if (items != null) {
        foreach (var item in items) {
          var filePath = Path.Combine(Db.Directories.Single(x => x.Id == item.DirectoryId).Path, item.FileName);
          if (!File.Exists(filePath)) continue;
          Picture pic = new Picture(filePath, Db, Items.Count, WbThumbs, item);
          pic.LoadFromDb(ACore, item);
          Items.Add(pic);
        }
      }

      ACore.UpdateStatusBarInfo();
    }

    public void LoadByTag() {
      /*Current = null;
      Items.Clear();
      var peopleIn = string.Join(",", ACore.TagModifers.OfType<Person>().Where(x => x.IsSelected).Select(x => x.Id));
      var peopleOut = string.Join(",", ACore.TagModifers.OfType<Person>().Where(x => !x.IsSelected).Select(x => x.Id));
      var keywordsIn = string.Join(",", ACore.TagModifers.OfType<Keyword>().Where(x => x.IsSelected).Select(x => x.Id));
      var keywordsOut = string.Join(",", ACore.TagModifers.OfType<Keyword>().Where(x => !x.IsSelected).Select(x => x.Id));

      List<string> sqlList = new List<string>();

      if (!string.IsNullOrEmpty(peopleIn))
        sqlList.Add($"select MediaItemId from MediaItemPerson where PersonId in ({peopleIn})");
      if (!string.IsNullOrEmpty(peopleOut))
        sqlList.Add($"select MediaItemId from MediaItemPerson where MediaItemId not in (select MediaItemId from MediaItemPerson where PersonId in ({peopleOut}))");
      if (!string.IsNullOrEmpty(keywordsIn))
        sqlList.Add($"select MediaItemId from MediaItemKeyword where KeywordId in ({keywordsIn})");
      if (!string.IsNullOrEmpty(keywordsOut))
        sqlList.Add($"select MediaItemId from MediaItemKeyword where MediaItemId not in (select MediaItemId from MediaItemKeyword where KeywordId in ({keywordsOut}))");
      if (ACore.LastSelectedSourceRecursive && ACore.LastSelectedSource is Keyword)
        sqlList.Add($"select MediaItemId from MediaItemKeyword where KeywordId in (select Id from Keywords where Name like \"{((Keyword)ACore.LastSelectedSource).FullPath}%\")");
      var folderKeyword = ACore.LastSelectedSource as FolderKeyword;
      if (!string.IsNullOrEmpty(folderKeyword?.FolderIds))
        sqlList.Add($"select Id from MediaItems where DirectoryId in ({folderKeyword.FolderIds})");

      string innerSql = string.Join(" union ", sqlList);
      string sql = "select Id, (select Path from Directories as D where D.Id = M.DirectoryId) as Path, FileName, Rating, DirectoryId, Comment, Orientation " +
                   $"from MediaItems as M where M.Id in ({innerSql}) order by FileName";

      foreach (DataRow row in Db.Select(sql)) {
        string picFullPath = Path.Combine((string)row[1], (string)row[2]);
        if (File.Exists(picFullPath)) {
          Picture pic = new Picture(picFullPath, Db, Items.Count, WbThumbs, null) {
            Id = (int)(long)row[0],
            Rating = (int)(long)row[3],
            DirId = (int)(long)row[4],
            Comment = (string)row[5],
            Orientation = (int)(long)row[6],
            FolderKeyword = ACore.FolderKeywords.GetFolderKeywordByFullPath((string)row[1])
          };

          pic.LoadFromDb(ACore);
          Items.Add(pic);
        }
      }

      ACore.UpdateStatusBarInfo();*/
    }

    public void ScrollToCurrent() {
      if (Current == null || Current.Index == 0) return;
      ScrollTo(Current.Index);
    }

    public void ScrollTo(int index) {
      WbThumbs.Document?.GetElementById(index.ToString())?.ScrollIntoView(true);
    }

    public void LoadSiblings() {
      if (!ACore.OneFileOnly) return;
      var filePath = Items[0].FilePath;
      LoadByFolder(Path.GetDirectoryName(filePath));
      var mi = Items.FirstOrDefault(p => p.FilePath == filePath);
      if (mi != null) mi.IsSelected = true;
      ACore.OneFileOnly = false;
    }

    public void RemoveSelectedFromWeb() {
      var doc = WbThumbs.Document;
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
  }
}
