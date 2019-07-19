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

    public void ClearBeforeLoad() {
      All.Clear();
    }

    public void AddRecord(BaseMediaItem record) {
      All.Add(record);
    }

    public void Delete(BaseMediaItem item) {
      foreach (var person in item.People)
        person.MediaItems.Remove(item);

      foreach (var keyword in item.Keywords)
        keyword.MediaItems.Remove(item);

      item.Folder.MediaItems.Remove(item);

      All.Remove(item);

      Helper.IsModifed = true;
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
              if (k.IsMarked) {
                //TODO DEBUG this
                //remove potencial redundant keywords (example: if new keyword is "#CoSpi/Sunny" keyword "#CoSpi" is redundant)
                for (var i = mi.Keywords.Count - 1; i >= 0; i--) {
                  if (k.FullPath.StartsWith(mi.Keywords[i].Title)) {
                    mi.Keywords.RemoveAt(i);
                  }
                }

                mi.Keywords.Add(k);
              }
              else
                mi.Keywords.Remove(k);

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
        if (delete) Delete(item);
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
  }
}
