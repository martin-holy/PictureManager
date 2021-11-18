using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls;
using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Interfaces;
using MH.Utils;
using MH.Utils.Extensions;
using PictureManager.CustomControls;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models;
using PictureManager.Domain.Utils;
using PictureManager.Interfaces;
using PictureManager.Properties;
using PictureManager.UserControls;
using PictureManager.Utils;
using PictureManager.ViewModels.Tree;
using ObservableObject = MH.Utils.BaseClasses.ObservableObject;

namespace PictureManager.ViewModels {
  public class MediaItemsBaseVM : ObservableObject {
    private readonly Core _core;
    private readonly AppCore _coreVM;
    private readonly WorkTask _workTask = new();
    private VirtualizingWrapPanel _currentThumbsGridPanel;
    private readonly Dictionary<string, string> _dateFormats = new() { { "d", "d. " }, { "M", "MMMM " }, { "y", "yyyy" } };

    public MediaItemsM Model { get; }
    public Dictionary<int, MediaItemBaseVM> All { get; } = new();
    public MediaItemBaseVM Current => ToViewModel(Model.Current);

    public RelayCommand<object> ClearFiltersCommand { get; }

    public MediaItemsBaseVM(Core core, AppCore coreVM, MediaItemsM model) {
      _core = core;
      _coreVM = coreVM;
      Model = model;

      Model.PropertyChanged += (_, e) => {
        if (nameof(Model.Current).Equals(e.PropertyName))
          OnPropertyChanged(nameof(Current));
      };

      _core.Segments.SegmentPersonChangedEvent += (_, e) => SetInfoBox(e.Segment.MediaItem);

      ClearFiltersCommand = new(() => _ = ClearFilters());
    }

    public IEnumerable<MediaItemBaseVM> ToViewModel(IEnumerable<MediaItemM> items, bool create = true) =>
      items.Select(mi => ToViewModel(mi, create));

    public MediaItemBaseVM ToViewModel(MediaItemM mi, bool create = true) {
      if (mi == null) return null;
      if (All.TryGetValue(mi.Id, out var miBaseVM)) return miBaseVM;
      if (!create) return null;

      miBaseVM = new(mi);
      All.Add(mi.Id, miBaseVM);

      return miBaseVM;
    }

    // TODO rethink
    public void RegisterEvents() {
      App.WMain.MainTabs.OnTabItemClose += (o, _) => {
        if (o is not TabItem { DataContext: ThumbnailsGridM grid } tab) return;

        (tab.Content as VirtualizingWrapPanel)?.ClearRows();
        grid.ClearItBeforeLoad();
        Model.ThumbnailsGrids.Remove(grid);
      };

      App.WMain.MainTabs.Tabs.SelectionChanged += async (o, _) => {
        var tabItem = ((TabControl)o).SelectedItem as TabItem;
        var grid = tabItem?.DataContext as ThumbnailsGridM;

        _currentThumbsGridPanel = grid == null ? null : (tabItem.Content as MediaItemsThumbsGrid)?.ThumbsGrid;

        Model.ThumbsGrid = ThumbnailsGridM.ActivateThumbnailsGrid(Model.ThumbsGrid, grid);
        grid?.UpdateSelected();
        if (grid?.NeedReload == true)
          await ThumbsGridReloadItems();
        App.Ui.MarkUsedKeywordsAndPeople();
      };
    }

    public void SetInfoBox(MediaItemM mi) => ToViewModel(mi)?.SetInfoBox();

    public void ScrollToCurrent() {
      if (Model.ThumbsGrid == null) return;

      if (Model.ThumbsGrid.Current == null)
        ScrollToTop();
      else {
        var miBaseVM = ToViewModel(Model.ThumbsGrid.Current);
        if (miBaseVM != null)
          ScrollTo(miBaseVM);
      }
    }

    public void ScrollToTop() {
      _currentThumbsGridPanel?.ScrollToTop();
      // TODO
      App.WMain.UpdateLayout();
    }

    public void ScrollTo(MediaItemBaseVM mi) => _currentThumbsGridPanel?.ScrollTo(mi);

    public void Delete(MediaItemM[] items) {
      if (items.Length == 0) return;
      var progress = new ProgressBarDialog(App.WMain, false, 1, "Removing Media Items from database ...");
      progress.AddEvents(items, null, Model.Delete, mi => mi.FilePath, null);
      progress.StartDialog();
    }

    // TODO rethink
    public void AddThumbsTabIfNotActive() {
      if (App.WMain.MainTabs.IsThisContentSet(typeof(MediaItemsThumbsGrid))) return;
      AddThumbsTab();
    }

    // TODO rethink
    public void AddThumbsTab() {
      var content = new MediaItemsThumbsGrid();
      var contextMenu = (ContextMenu)content.FindResource("ThumbsGridContextMenu");
      var dataContext = Model.AddThumbnailsGridModel();

      content.DataContext = dataContext;
      contextMenu.DataContext = dataContext;

      var tab = App.WMain.MainTabs.AddTab(IconName.Folder, content);
      tab.DataContext = dataContext;
      tab.IsSelected = true;
      tab.UpdateLayout();
      if (tab.FindChild<StackPanel>("TabHeader") is { } tabHeader)
        tabHeader.ContextMenu = contextMenu;

      _currentThumbsGridPanel = content.ThumbsGrid;
    }

    public void SetMetadata(ICatTreeViewTagItem item) {
      foreach (var mi in Model.ThumbsGrid.SelectedItems) {
        Model.SetModified(mi, true);

        switch (item) {
          case PersonTreeVM p:
            mi.People = ListExtensions.Toggle(mi.People, p.BaseVM.Model, true);
            break;

          case KeywordTreeVM k:
            mi.Keywords = KeywordsM.Toggle(mi.Keywords, k.BaseVM.Model);
            break;

          case RatingTreeVM r:
            mi.Rating = r.Value;
            break;

          case GeoNameTreeVM g:
            mi.GeoName = g.Model;
            break;
        }

        SetInfoBox(mi);
      }
    }

    public async Task LoadByTag(ICatTreeViewItem item, bool and, bool hide, bool recursive) {
      var items = item switch {
        RatingTreeVM rating => Model.All.Where(x => x.Rating == rating.Value).ToList(),
        PersonTreeVM person => _core.PeopleM.GetMediaItems(person.BaseVM.Model),
        KeywordTreeVM keyword => _core.KeywordsM.GetMediaItems(keyword.BaseVM.Model, recursive),
        GeoNameTreeVM geoName => _core.GeoNamesM.GetMediaItems(geoName.Model, recursive).OrderBy(x => x.FileName).ToList(),
        _ => new()
      };

      var tabTitle = and || hide
        ? Model.ThumbsGrid.Title
        : item switch {
          RatingTreeVM rating => rating.Value.ToString(),
          PersonTreeVM person => person.BaseVM.Model.Name,
          KeywordTreeVM keyword => keyword.BaseVM.Model.Name,
          GeoNameTreeVM geoName => geoName.Model.Name,
          _ => string.Empty
        };

      await LoadMediaItems(items, and, hide, tabTitle);
    }

    public async Task LoadByFolder(ICatTreeViewItem item, bool and, bool hide, bool recursive) {
      if (item is FolderTreeVM folder && !folder.Model.IsAccessible) return;

      item.IsSelected = true;

      if (_coreVM.AppInfo.AppMode == AppMode.Viewer)
        Commands.WindowCommands.SwitchToBrowser();

      var roots = (item as FolderKeywordTreeVM)?.Model.Folders ?? new List<FolderM> { ((FolderTreeVM)item).Model };
      var folders = FoldersM.GetFolders(roots, recursive).Where(f => !_coreVM.FoldersTreeVM.All[f.Id].IsHidden).ToList();

      if (and || hide) {
        var items = folders.SelectMany(x => x.MediaItems).ToList();
        await LoadMediaItems(items, and, hide, Model.ThumbsGrid.Title);
        return;
      }

      await LoadAsync(null, folders, folders[0].Name);
      // TODO move this up, check for changes before update
      App.Ui.MarkUsedKeywordsAndPeople();
    }

    private async Task LoadMediaItems(List<MediaItemM> items, bool and, bool hide, string tabTitle) {
      // if CTRL is pressed, add new items to already loaded items
      if (and)
        items = Model.ThumbsGrid.LoadedItems.Union(items).ToList();

      // if ALT is pressed, remove new items from already loaded items
      if (hide)
        items = Model.ThumbsGrid.LoadedItems.Except(items).ToList();

      await LoadAsync(items, null, tabTitle);
      // TODO move this up, check for changes before update
      App.Ui.MarkUsedKeywordsAndPeople();
    }

    private async Task LoadAsync(List<MediaItemM> mediaItems, List<FolderM> folders, string tabTitle) {
      await _workTask.Cancel();
      ScrollToTop();
      Model.ThumbsGrid.Title = tabTitle;

      // Clear before new load
      Model.ThumbsGrid.ClearItBeforeLoad();
      // TODO move this elsewhere
      App.WMain.ImageComparerTool.Close();
      // TODO set this to false when finished
      _coreVM.AppInfo.ProgressBarIsIndeterminate = true;

      await _workTask.Start(Task.Run(async () => {
        var items = new List<MediaItemM>();

        if (mediaItems != null)
          // filter out items if directory or file not exists or Viewer can not see items
          items = await Model.VerifyAccessibilityOfMediaItemsAsync(mediaItems, _workTask.Token);

        if (folders != null)
          items = await Model.GetMediaItemsFromFoldersAsync(folders, _workTask.Token);

        // set thumb size and add Media Items to LoadedItems
        foreach (var mi in items) {
          mi.SetThumbSize();
          Model.ThumbsGrid.LoadedItems.Add(mi);
        }

        await Model.ThumbsGrid.ReloadFilteredItems(Filter(Model.ThumbsGrid.LoadedItems));
        await LoadThumbnailsAsync(Model.ThumbsGrid.FilteredItems.ToArray(), _workTask.Token);
        SetMediaItemSizesLoadedRange();
      }));
    }

    public async Task ThumbsGridReloadItems() {
      if (!await _workTask.Cancel()) return;

      ScrollToTop();
      _currentThumbsGridPanel?.ClearRows();

      if (Model.ThumbsGrid == null || Model.ThumbsGrid.FilteredItems.Count == 0) return;

      await _workTask.Start(Task.Run(async () =>
        await LoadThumbnailsAsync(Model.ThumbsGrid.FilteredItems.ToArray(), _workTask.Token)));

      Model.ThumbsGrid.NeedReload = false;
      ScrollToCurrent();
    }

    private async Task LoadThumbnailsAsync(IReadOnlyCollection<MediaItemM> items, CancellationToken token) {
      _coreVM.AppInfo.ProgressBarIsIndeterminate = false;
      _coreVM.AppInfo.ResetProgressBars(100);

      await Task.Run(async () => {
        // read metadata for new items and add thumbnails to grid
        var metadata = ReadMetadataAndListThumbsAsync(items, token);
        // create thumbnails
        var progress = new Progress<int>(x => App.Ui.AppInfo.ProgressBarValueB = x);
        var thumbs = Imaging.CreateThumbnailsAsync(items, Settings.Default.ThumbnailSize, Settings.Default.JpegQualityLevel, progress, token);
        
        await Task.WhenAll(metadata, thumbs);

        if (token.IsCancellationRequested)
          await _core.RunOnUiThread(() => Delete(Model.All.Where(x => x.IsNew).ToArray()));
      }, token);

      // TODO: is this necessary?
      if (Model.ThumbsGrid?.Current != null) {
        Model.ThumbsGrid.SetSelected(Model.ThumbsGrid.Current, false);
        Model.ThumbsGrid.SetSelected(Model.ThumbsGrid.Current, true);
      }

      _coreVM.AppInfo.ProgressBarValueA = 100;
      _coreVM.AppInfo.ProgressBarValueB = 100;

      GC.Collect();
    }

    private async Task ReadMetadataAndListThumbsAsync(IReadOnlyCollection<MediaItemM> items, CancellationToken token) {
      await _core.RunOnUiThread(() => _currentThumbsGridPanel.ClearRows());

      await Task.Run(async () => {
        var count = items.Count;
        var workingOn = 0;

        foreach (var mi in items) {
          if (token.IsCancellationRequested) break;

          workingOn++;
          var percent = Convert.ToInt32((double)workingOn / count * 100);

          if (mi.IsNew) {
            mi.IsNew = false;

            var success = await ReadMetadata(mi);
            if (!success) {
              // delete corrupted MediaItems
              await _core.RunOnUiThread(() => {
                Model.ThumbsGrid.LoadedItems.Remove(mi);
                Model.ThumbsGrid.FilteredItems.Remove(mi);
                Model.Delete(mi);
                _coreVM.AppInfo.ProgressBarValueA = percent;
              });

              continue;
            }
          }

          await AddMediaItemToGrid(mi);

          await _core.RunOnUiThread(() => {
            SetInfoBox(mi);
            _coreVM.AppInfo.ProgressBarValueA = percent;
          });
        }
      }, token);
    }

    public async Task<bool> ReadMetadata(MediaItemM mi, bool gpsOnly = false) {
      try {
        if (mi.MediaType == MediaType.Video) {
          await _core.RunOnUiThread(() => ReadVideoMetadata(mi));
        }
        else {
          using Stream srcFileStream = File.Open(mi.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
          var decoder = BitmapDecoder.Create(srcFileStream, BitmapCreateOptions.None, BitmapCacheOption.None);
          var frame = decoder.Frames[0];
          mi.Width = frame.PixelWidth;
          mi.Height = frame.PixelHeight;
          mi.SetThumbSize(true);

          Model.DataAdapter.IsModified = true;

          // true because only media item dimensions are required
          if (frame.Metadata is not BitmapMetadata bm) return true;

          ReadImageMetadata(mi, bm, gpsOnly);

          mi.SetThumbSize(true);
        }
      }
      catch (Exception ex) {
        _core.LogError(ex, mi.FilePath);

        // No imaging component suitable to complete this operation was found.
        if ((ex.InnerException as COMException)?.HResult == -2003292336)
          return false;

        mi.IsOnlyInDb = true;

        // true because only media item dimensions are required
        return true;
      }

      return true;
    }

    private void ReadVideoMetadata(MediaItemM mi) {
      try {
        var size = ShellStuff.FileInformation.GetVideoMetadata(mi.Folder.FullPath, mi.FileName);
        mi.Height = (int)size[0];
        mi.Width = (int)size[1];
        mi.Orientation = (int)size[2] switch {
          90 => (int)MediaOrientation.Rotate90,
          180 => (int)MediaOrientation.Rotate180,
          270 => (int)MediaOrientation.Rotate270,
          _ => (int)MediaOrientation.Normal,
        };
        mi.SetThumbSize(true);

        Model.DataAdapter.IsModified = true;
      }
      catch (Exception ex) {
        _core.LogError(ex, mi.FilePath);
      }
    }

    private async void ReadImageMetadata(MediaItemM mi, BitmapMetadata bm, bool gpsOnly) {
      // Lat Lng
      var tmpLat = bm.GetQuery("System.GPS.Latitude.Proxy")?.ToString();
      if (tmpLat != null) {
        var vals = tmpLat[..^1].Split(',');
        mi.Lat = (int.Parse(vals[0]) + (double.Parse(vals[1], CultureInfo.InvariantCulture) / 60)) * (tmpLat.EndsWith("S") ? -1 : 1);
      }

      var tmpLng = bm.GetQuery("System.GPS.Longitude.Proxy")?.ToString();
      if (tmpLng != null) {
        var vals = tmpLng[..^1].Split(',');
        mi.Lng = (int.Parse(vals[0]) + (double.Parse(vals[1], CultureInfo.InvariantCulture) / 60)) * (tmpLng.EndsWith("W") ? -1 : 1);
      }

      if (gpsOnly) return;

      // People
      mi.People = null;
      const string microsoftRegions = "/xmp/MP:RegionInfo/MPRI:Regions";
      const string microsoftPersonDisplayName = "/MPReg:PersonDisplayName";

      if (bm.GetQuery(microsoftRegions) is BitmapMetadata regions && regions.Any()) {
        mi.People = new(regions.Count());
        foreach (var region in regions) {
          var personDisplayName = bm.GetQuery(microsoftRegions + region + microsoftPersonDisplayName);
          if (personDisplayName != null)
            mi.People.Add(await _core.RunOnUiThread(() =>
              _core.PeopleM.GetPerson(personDisplayName.ToString(), true)));
        }
      }

      // Rating
      mi.Rating = bm.Rating;

      // Comment
      mi.Comment = StringUtils.NormalizeComment(bm.Comment);

      // Orientation 1: 0, 3: 180, 6: 270, 8: 90
      mi.Orientation = (ushort?)bm.GetQuery("System.Photo.Orientation") ?? 1;

      // Keywords
      mi.Keywords = null;
      if (bm.Keywords != null) {
        mi.Keywords = new();
        // Filter out duplicities
        foreach (var k in bm.Keywords.OrderByDescending(x => x)) {
          if (mi.Keywords.SingleOrDefault(x => x.FullName.Equals(k)) != null) continue;
          await _core.RunOnUiThread(() => {
            var keyword = _core.KeywordsM.GetByFullPath(k);
            if (keyword != null)
              mi.Keywords.Add(keyword);
          });
        }
      }

      // GeoNameId
      var tmpGId = bm.GetQuery("/xmp/GeoNames:GeoNameId");
      // TODO change condition
      if (!string.IsNullOrEmpty(tmpGId as string)) {
        // TODO find/create GeoName
        mi.GeoName = _core.GeoNamesM.All.SingleOrDefault(x => x.Id == int.Parse(tmpGId.ToString()));
      }
    }

    private async Task AddMediaItemToGrid(MediaItemM mi) {
      const int itemOffset = 6; //border, margin, padding, ... //TODO find the real value
      var groupItems = new List<VirtualizingWrapPanelGroupItem>();

      if (Model.ThumbsGrid.GroupByFolders) {
        var folderName = mi.Folder.Name;
        var iOfL = folderName.FirstIndexOfLetter();
        var title = iOfL == 0 || folderName.Length - 1 == iOfL ? folderName : folderName[iOfL..];
        var toolTip = mi.Folder.FolderKeyword != null
          ? mi.Folder.FolderKeyword.FullPath
          : mi.Folder.FullPath;
        groupItems.Add(new() { Icon = IconName.Folder, Title = title, ToolTip = toolTip });
      }

      if (Model.ThumbsGrid.GroupByDate) {
        var title = DateTimeExtensions.DateTimeFromString(mi.FileName, _dateFormats, null);
        if (!string.IsNullOrEmpty(title))
          groupItems.Add(new() { Icon = IconName.Calendar, Title = title });
      }

      await _core.RunOnUiThread(() => {
        var miBaseVM = ToViewModel(mi);
        _currentThumbsGridPanel.AddGroupIfNew(groupItems.ToArray());
        _currentThumbsGridPanel.AddItem(miBaseVM, mi.ThumbWidth + itemOffset);
      });
    }

    public async Task ActivateFilter(IFilterItem item, DisplayFilter displayFilter) {
      Model.ThumbsGrid?.SetDisplayFilter(item, displayFilter);
      await ReapplyFilter();
    }

    public async Task ReapplyFilter() {
      if (Model.ThumbsGrid != null)
        await Model.ThumbsGrid.ReloadFilteredItems(Filter(Model.ThumbsGrid.LoadedItems));
      _coreVM.MarkUsedKeywordsAndPeople();
      await ThumbsGridReloadItems();
    }

    private async Task ClearFilters() {
      Model.ThumbsGrid?.ClearFilters();
      await ReapplyFilter();
    }

    public IEnumerable<MediaItemM> Filter(List<MediaItemM> mediaItems) =>
      Model.ThumbsGrid.Filter(mediaItems,
        _coreVM.MediaItemSizesTreeVM.Size.AllSizes()
          ? null
          : _coreVM.MediaItemSizesTreeVM.Size.Fits);

    private void SetMediaItemSizesLoadedRange() {
      var zeroItems = Model.ThumbsGrid == null || Model.ThumbsGrid.FilteredItems.Count == 0;
      var min = zeroItems ? 0 : Model.ThumbsGrid.FilteredItems.Min(x => x.Width * x.Height);
      var max = zeroItems ? 0 : Model.ThumbsGrid.FilteredItems.Max(x => x.Width * x.Height);
      _coreVM.MediaItemSizesTreeVM.Size.SetLoadedRange(min, max);
    }

    public void SetOrientation(MediaItemM[] mediaItems, Rotation rotation) {
      // TODO ProgressBarDialog(App.WMain
      var progress = new ProgressBarDialog(App.WMain, true, Environment.ProcessorCount, "Changing orientation ...");
      progress.AddEvents(
        mediaItems,
        null,
        // action
        async mi => {
          var newOrientation = mi.RotationAngle;

          if (mi.MediaType == MediaType.Image) {
            switch (rotation) {
              case Rotation.Rotate90: newOrientation += 90; break;
              case Rotation.Rotate180: newOrientation += 180; break;
              case Rotation.Rotate270: newOrientation += 270; break;
            }
          }
          else if (mi.MediaType == MediaType.Video) {
            // images have switched 90 and 270 angles and all application is made with this in mind
            // so I switched orientation just for video
            switch (rotation) {
              case Rotation.Rotate90: newOrientation += 270; break;
              case Rotation.Rotate180: newOrientation += 180; break;
              case Rotation.Rotate270: newOrientation += 90; break;
            }
          }

          if (newOrientation >= 360) newOrientation -= 360;

          switch (newOrientation) {
            case 0: mi.Orientation = (int)MediaOrientation.Normal; break;
            case 90: mi.Orientation = (int)MediaOrientation.Rotate90; break;
            case 180: mi.Orientation = (int)MediaOrientation.Rotate180; break;
            case 270: mi.Orientation = (int)MediaOrientation.Rotate270; break;
          }

          TryWriteMetadata(mi);
          mi.SetThumbSize(true);
          await Imaging.CreateThumbnailAsync(mi.MediaType, mi.FilePath, mi.FilePathCache, mi.ThumbSize, mi.RotationAngle, Settings.Default.JpegQualityLevel);
          mi.ReloadThumbnail();
          await _core.RunOnUiThread(() => Model.DataAdapter.IsModified = true);
        },
        mi => mi.FilePath,
        // onCompleted
        (o, e) => _ = ThumbsGridReloadItems());

      progress.StartDialog();
    }

    public bool TryWriteMetadata(MediaItemM mediaItem) {
      if (mediaItem.IsOnlyInDb) return true;
      try {
        return WriteMetadata(mediaItem) ? true : throw new("Error writing metadata");
      }
      catch (Exception ex) {
        _core.LogError(ex, $"Metadata will be saved just in Database. {mediaItem.FilePath}");
        // set MediaItem as IsOnlyInDb to not save metadata to file, but keep them just in DB
        mediaItem.IsOnlyInDb = true;
        return false;
      }
    }

    private bool WriteMetadata(MediaItemM mi) {
      if (mi.MediaType == MediaType.Video) return true;
      var original = new FileInfo(mi.FilePath);
      var newFile = new FileInfo(mi.FilePath + "_newFile");
      var bSuccess = false;
      const BitmapCreateOptions createOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;

      using (Stream originalFileStream = File.Open(original.FullName, FileMode.Open, FileAccess.Read)) {
        var decoder = BitmapDecoder.Create(originalFileStream, createOptions, BitmapCacheOption.None);
        if (decoder.CodecInfo?.FileExtensions.Contains("jpg") == true && decoder.Frames[0] != null) {
          var metadata = decoder.Frames[0].Metadata == null
            ? new("jpg")
            : decoder.Frames[0].Metadata.Clone() as BitmapMetadata;

          if (metadata != null) {
            //People
            if (mi.People != null) {
              const string microsoftRegionInfo = "/xmp/MP:RegionInfo";
              const string microsoftRegions = "/xmp/MP:RegionInfo/MPRI:Regions";
              const string microsoftPersonDisplayName = "/MPReg:PersonDisplayName";
              var peopleIdx = -1;
              var addedPeople = new List<string>();
              //New metadata just for People
              var people = new BitmapMetadata("jpg");
              people.SetQuery(microsoftRegionInfo, new BitmapMetadata("xmpstruct"));
              people.SetQuery(microsoftRegions, new BitmapMetadata("xmpbag"));
              //Adding existing people => preserve original metadata because they can contain positions of people
              if (metadata.GetQuery(microsoftRegions) is BitmapMetadata existingPeople) {
                foreach (var idx in existingPeople) {
                  var existingPerson = metadata.GetQuery(microsoftRegions + idx) as BitmapMetadata;
                  var personDisplayName = existingPerson?.GetQuery(microsoftPersonDisplayName);
                  if (personDisplayName == null) continue;
                  if (!mi.People.Any(p => p.Name.Equals(personDisplayName.ToString()))) continue;
                  addedPeople.Add(personDisplayName.ToString());
                  peopleIdx++;
                  people.SetQuery($"{microsoftRegions}/{{ulong={peopleIdx}}}", existingPerson);
                }
              }

              //Adding new people
              foreach (var person in mi.People.Where(p => !addedPeople.Any(ap => ap.Equals(p.Name)))) {
                peopleIdx++;
                people.SetQuery($"{microsoftRegions}/{{ulong={peopleIdx}}}", new BitmapMetadata("xmpstruct"));
                people.SetQuery($"{microsoftRegions}/{{ulong={peopleIdx}}}" + microsoftPersonDisplayName, person.Name);
              }

              //Writing all people to MediaItem metadata
              var allPeople = people.GetQuery(microsoftRegionInfo);
              if (allPeople != null)
                metadata.SetQuery(microsoftRegionInfo, allPeople);
            }

            metadata.Rating = mi.Rating;
            metadata.Comment = mi.Comment ?? string.Empty;
            metadata.Keywords = new(mi.Keywords?.Select(k => k.FullName).ToList() ?? new List<string>());
            metadata.SetQuery("System.Photo.Orientation", (ushort)mi.Orientation);

            //GeoNameId
            if (mi.GeoName == null)
              metadata.RemoveQuery("/xmp/GeoNames:GeoNameId");
            else
              metadata.SetQuery("/xmp/GeoNames:GeoNameId", mi.GeoName.Id.ToString());

            bSuccess = WriteMetadataToFile(mi, newFile, decoder, metadata, true);
          }
        }
      }

      if (bSuccess) {
        newFile.CreationTime = original.CreationTime;
        original.Delete();
        newFile.MoveTo(original.FullName);
      }
      return bSuccess;
    }

    private bool WriteMetadataToFile(MediaItemM mi, FileInfo newFile, BitmapDecoder decoder, BitmapMetadata metadata, bool withThumbnail) {
      bool bSuccess;
      var hResult = 0;
      var encoder = new JpegBitmapEncoder { QualityLevel = Settings.Default.JpegQualityLevel };
      encoder.Frames.Add(BitmapFrame.Create(decoder.Frames[0],
        withThumbnail ? decoder.Frames[0].Thumbnail : null, metadata, decoder.Frames[0].ColorContexts));

      try {
        using Stream newFileStream = File.Open(newFile.FullName, FileMode.Create, FileAccess.ReadWrite);
        encoder.Save(newFileStream);

        bSuccess = true;
      }
      catch (Exception ex) {
        bSuccess = false;
        hResult = ex.HResult;

        // don't log error if hResult is -2146233033
        if (hResult != -2146233033)
          _core.LogError(ex, mi.FilePath);
      }

      // There is too much metadata to be written to the bitmap. (Exception from HRESULT: 0x88982F52)
      // It might be problem with ThumbnailImage in JPEG images taken by Huawei P10
      if (!bSuccess && hResult == -2146233033) {
        if (metadata.ContainsQuery("/app1/thumb/"))
          metadata.RemoveQuery("/app1/thumb/");
        else {
          // removing thumbnail wasn't enough
          return false;
        }

        bSuccess = WriteMetadataToFile(mi, newFile, decoder, metadata, false);
      }

      return bSuccess;
    }

    /// <summary>
    /// Copy or Move MediaItems (Files, Cache and DB)
    /// </summary>
    /// <param name="mode"></param>
    /// <param name="items"></param>
    /// <param name="destFolder"></param>
    public void CopyMove(FileOperationMode mode, List<MediaItemM> items, FolderM destFolder) {
      var fop = new FileOperationDialog(App.WMain, mode) { PbProgress = { IsIndeterminate = false, Value = 0 } };
      fop.RunTask = Task.Run(() => {
        fop.LoadCts = new();
        var token = fop.LoadCts.Token;

        try {
          Model.CopyMove(mode, items, destFolder, fop.Progress,
            (string srcFilePath, string destFilePath, ref string destFileName) =>
              AppCore.ShowFileOperationCollisionDialog(srcFilePath, destFilePath, fop, ref destFileName), token);
        }
        catch (Exception ex) {
          ErrorDialog.Show(ex);
        }
      }).ContinueWith(_ => _core.RunOnUiThread(() => fop.Close()));

      _ = fop.ShowDialog();

      if (mode == FileOperationMode.Move) {
        Model.ThumbsGrid.RemoveSelected();
        _ = ThumbsGridReloadItems();
      }
    }
  }
}
