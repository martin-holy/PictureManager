﻿using PictureManager.Commands;
using PictureManager.CustomControls;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Models;
using PictureManager.Domain.Utils;
using PictureManager.Properties;
using PictureManager.UserControls;
using PictureManager.Utils;
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

namespace PictureManager.ViewModels {
  public class MediaItemsViewModel {
    private CancellationTokenSource _loadCts;
    private Task _loadTask;
    private readonly MediaItems _model;
    private readonly Dictionary<string, string> _dateFormats = new() { { "d", "d. " }, { "M", "MMMM " }, { "y", "yyyy" } };

    public VirtualizingWrapPanel CurrentThumbsGrid { get; set; }

    public MediaItemsViewModel(Core core) {
      _model = core.MediaItems;
    }

    ~MediaItemsViewModel() {
      if (_loadCts != null) {
        _loadCts.Dispose();
        _loadCts = null;
      }
    }

    public void RegisterEvents() {
      App.WMain.MainTabs.OnTabItemClose += (o, e) => {
        if (o is not TabItem tab || tab.DataContext is not ThumbnailsGrid grid) return;

        (tab.Content as VirtualizingWrapPanel)?.ClearRows();
        grid.ClearItBeforeLoad();
        _model.ThumbnailsGrids.Remove(grid);
      };

      App.WMain.MainTabs.Tabs.SelectionChanged += (o, e) => {
        var tabItem = ((TabControl)o).SelectedItem as TabItem;
        var grid = tabItem?.DataContext as ThumbnailsGrid;

        CurrentThumbsGrid = grid == null ? null : (tabItem.Content as MediaItemsThumbsGrid)?.ThumbsGrid;

        _model.ThumbsGrid = grid;
        grid?.UpdateSelected();
        if (grid?.NeedReload == true)
          ThumbsGridReloadItems();
        App.Core.MarkUsedKeywordsAndPeople();
      };
    }

    private ThumbnailsGrid AddThumbnailsGridModel() {
      var grid = new ThumbnailsGrid();
      _model.ThumbnailsGrids.Add(grid);
      _model.ThumbsGrid = grid;

      return grid;
    }

    public void SetTabContent() {
      if (App.WMain.MainTabs.IsThisContentSet(typeof(MediaItemsThumbsGrid))) return;

      var dataContext = AddThumbnailsGridModel();
      var content = new MediaItemsThumbsGrid { DataContext = dataContext };
      var contextMenu = (ContextMenu)content.FindResource("ThumbsGridContextMenu");
      contextMenu.DataContext = dataContext;
      CurrentThumbsGrid = content.ThumbsGrid;
      App.WMain.MainTabs.SetTab(dataContext, content, contextMenu);
    }

    public static void SetMetadata(ICatTreeViewItem item) {
      if (item is not ICatTreeViewTagItem bti) return;

      bti.IsMarked = !bti.IsMarked;
      if (bti.IsMarked) {
        App.Core.MarkedTags.Add(bti);
      }
      else {
        App.Core.MarkedTags.Remove(bti);
        bti.PicCount = 0;
      }

      App.Core.MediaItems.SetMetadata(item);

      App.Core.MarkUsedKeywordsAndPeople();
      App.WMain.StatusPanel.UpdateRating();
    }

    public async void LoadByTag(ICatTreeViewItem item, bool and, bool hide, bool recursive) {
      List<MediaItem> items = item switch {
        Rating rating => App.Core.MediaItems.All.Cast<MediaItem>().Where(x => x.Rating == rating.Value).ToList(),
        Person person => person.GetMediaItems().ToList(),
        Keyword keyword => keyword.GetMediaItems(recursive).ToList(),
        GeoName geoName => geoName.GetMediaItems(recursive).ToList(),
        _ => new()
      };

      // if CTRL is pressed, add new items to already loaded items
      if (and)
        items = App.Core.MediaItems.ThumbsGrid.LoadedItems.Union(items).ToList();

      // if ALT is pressed, remove new items from already loaded items
      if (hide)
        items = App.Core.MediaItems.ThumbsGrid.LoadedItems.Except(items).ToList();

      await LoadAsync(items, null, item.Title);
      App.Core.MarkUsedKeywordsAndPeople();
    }

    public async void LoadByFolder(ICatTreeViewItem item, bool and, bool hide, bool recursive) {
      if (item is Folder folder && !folder.IsAccessible) return;

      item.IsSelected = true;

      if (App.Ui.AppInfo.AppMode == AppMode.Viewer)
        WindowCommands.SwitchToBrowser();

      var roots = (item as FolderKeyword)?.Folders ?? new List<Folder> { (Folder)item };
      var folders = Folder.GetFolders(roots, recursive);

      // if CTRL is pressed, add items from new folders to already loaded items
      if (and)
        folders = App.Core.MediaItems.ThumbsGrid.LoadedItems.Select(x => x.Folder).Distinct().Union(folders).ToList();

      // if ALT is pressed, remove items from new folders from already loaded items
      if (hide)
        folders = App.Core.MediaItems.ThumbsGrid.LoadedItems.Select(x => x.Folder).Distinct().Except(folders).ToList();

      await LoadAsync(null, folders, folders[0].Title);
      App.Core.MarkUsedKeywordsAndPeople();
    }

    public async Task LoadAsync(List<MediaItem> mediaItems, List<Folder> folders, string tabTitle) {
      // cancel previous work
      if (_loadCts != null) {
        _loadCts.Cancel();
        await _loadTask;
      }

      ScrollToTop();

      var currentGrid = _model.ThumbsGrid;
      currentGrid.Title = tabTitle;

      // Clear before new load
      currentGrid.ClearItBeforeLoad();
      App.WMain.ImageComparerTool.Close();

      App.Ui.AppInfo.ProgressBarIsIndeterminate = true;

      _loadTask = Task.Run(async () => {
        _loadCts?.Dispose();
        _loadCts = new();
        var token = _loadCts.Token;
        var items = new List<MediaItem>();

        if (mediaItems != null)
          // filter out items if directory or file not exists or Viewer can not see items
          items = await MediaItems.VerifyAccessibilityOfMediaItemsAsync(mediaItems, token);

        if (folders != null)
          items = await _model.GetMediaItemsFromFoldersAsync(folders, token);

        // set thumb size and add Media Items to LoadedItems
        foreach (var mi in items) {
          mi.SetThumbSize();
          currentGrid.LoadedItems.Add(mi);
        }

        currentGrid.ReloadFilteredItems();

        await LoadThumbnailsAsync(currentGrid.FilteredItems.ToArray(), token);
        App.Core.SetMediaItemSizesLoadedRange();
      });

      await _loadTask;
    }

    private async Task LoadThumbnailsAsync(IReadOnlyCollection<MediaItem> items, CancellationToken token) {
      App.Ui.AppInfo.ProgressBarIsIndeterminate = false;
      App.Ui.AppInfo.ResetProgressBars(100);

      await Task.Run(async () => {
        // read metadata for new items and add thumbnails to grid
        var metadata = ReadMetadataAndListThumbsAsync(items, token);
        // create thumbnails
        var progress = new Progress<int>(x => App.Ui.AppInfo.ProgressBarValueB = x);
        var thumbs = Imaging.CreateThumbnailsAsync(items, token, Settings.Default.ThumbnailSize, Settings.Default.JpegQualityLevel, progress);

        await Task.WhenAll(metadata, thumbs);

        if (token.IsCancellationRequested)
          await App.Core.RunOnUiThread(() => {
            Delete(_model.All.Cast<MediaItem>().Where(x => x.IsNew).ToArray());
          });
      }, token);

      // TODO: is this necessary?
      if (_model.ThumbsGrid?.Current != null) {
        _model.ThumbsGrid.SetSelected(_model.ThumbsGrid.Current, false);
        _model.ThumbsGrid.SetSelected(_model.ThumbsGrid.Current, true);
      }

      App.Ui.AppInfo.ProgressBarValueA = 100;
      App.Ui.AppInfo.ProgressBarValueB = 100;

      GC.Collect();
    }

    private Task<bool> ReadMetadataAndListThumbsAsync(IReadOnlyCollection<MediaItem> items, CancellationToken token) {
      App.Core.RunOnUiThread(() => {
        CurrentThumbsGrid.ClearRows();
      });

      return Task.Run(() => {
        var mediaItemsModified = false;
        var count = items.Count;
        var workingOn = 0;

        foreach (var mi in items) {
          if (token.IsCancellationRequested) break;

          workingOn++;
          var percent = Convert.ToInt32((double)workingOn / count * 100);

          if (mi.IsNew) {
            mi.IsNew = false;

            App.Core.RunOnUiThread(() => { _model.MediaItemsCount++; });

            if (!ReadMetadata(mi)) {
              // delete corrupted MediaItems
              App.Core.RunOnUiThread(() => {
                _model.ThumbsGrid.LoadedItems.Remove(mi);
                _model.ThumbsGrid.FilteredItems.Remove(mi);
                _model.Delete(mi);
                App.Ui.AppInfo.ProgressBarValueA = percent;
              });

              continue;
            }

            mediaItemsModified = true;
          }

          AddMediaItemToGrid(mi);

          App.Core.RunOnUiThread(() => {
            mi.SetInfoBox();
            App.Ui.AppInfo.ProgressBarValueA = percent;
          });
        }

        return mediaItemsModified;
      });
    }

    private void AddMediaItemToGrid(MediaItem mi) {
      const int itemOffset = 6; //border, margin, padding, ... //TODO find the real value
      var groupItems = new List<VirtualizingWrapPanelGroupItem>();

      if (_model.ThumbsGrid.GroupByFolders) {
        var folderName = mi.Folder.Title;
        var iOfL = folderName.FirstIndexOfLetter();
        var title = iOfL == 0 || folderName.Length - 1 == iOfL ? folderName : folderName[iOfL..];
        var toolTip = mi.Folder.FolderKeyword != null
          ? CatTreeViewUtils.GetFullPath(mi.Folder.FolderKeyword, Path.DirectorySeparatorChar.ToString())
          : mi.Folder.FullPath;
        groupItems.Add(new() { Icon = IconName.Folder, Title = title, ToolTip = toolTip });
      }

      if (_model.ThumbsGrid.GroupByDate) {
        var title = Domain.Extensions.DateTimeFromString(mi.FileName, _dateFormats, null);
        if (!string.IsNullOrEmpty(title))
          groupItems.Add(new() { Icon = IconName.Calendar, Title = title });
      }

      App.Core.RunOnUiThread(() => {
        CurrentThumbsGrid.AddGroupIfNew(groupItems.ToArray());
        CurrentThumbsGrid.AddItem(mi, mi.ThumbWidth + itemOffset);
      });
    }

    public async void ThumbsGridReloadItems() {
      CurrentThumbsGrid?.ClearRows();
      if (_model.ThumbsGrid == null || _model.ThumbsGrid.FilteredItems.Count == 0) return;

      // cancel previous work
      if (_loadCts != null) {
        _loadCts.Cancel();
        await _loadTask;
      }

      //ScrollToTop();
      //App.WMain.UpdateLayout();

      _loadTask = Task.Run(async () => {
        _loadCts?.Dispose();
        _loadCts = new();
        var token = _loadCts.Token;
        await LoadThumbnailsAsync(_model.ThumbsGrid.FilteredItems.ToArray(), token);
      });

      await _loadTask;

      _model.ThumbsGrid.NeedReload = false;
      ScrollToCurrent();
    }

    public void ScrollToCurrent() {
      if (_model.ThumbsGrid?.Current == null)
        ScrollToTop();
      else
        ScrollTo(_model.ThumbsGrid?.Current);
    }

    public void ScrollToTop() {
      CurrentThumbsGrid?.ScrollToTop();
      App.WMain.UpdateLayout();
    }

    public void ScrollTo(MediaItem mi) => CurrentThumbsGrid?.ScrollTo(mi);

    public void SetOrientation(MediaItem[] mediaItems, Rotation rotation) {
      var progress = new ProgressBarDialog(App.WMain, true, Environment.ProcessorCount, "Changing orientation ...");
      progress.AddEvents(
        mediaItems,
        null,
        // action
        async delegate (MediaItem mi) {
          var newOrientation = mi.RotationAngle;

          if (mi.MediaType == MediaType.Image) {
            switch (rotation) {
              case Rotation.Rotate90: newOrientation += 90; break;
              case Rotation.Rotate180: newOrientation += 180; break;
              case Rotation.Rotate270: newOrientation += 270; break;
            }
          } else if (mi.MediaType == MediaType.Video) {
            // images have switched 90 and 270 angles and all app is made with this in mind
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
          await App.Core.RunOnUiThread(App.Db.SetModified<MediaItems>);
        },
        mi => mi.FilePath,
        // onCompleted
        delegate {
          ThumbsGridReloadItems();
        });

      progress.StartDialog();
    }

    /// <summary>
    /// Copy or Move MediaItems (Files, Cache and DB)
    /// </summary>
    /// <param name="mode"></param>
    /// <param name="items"></param>
    /// <param name="destFolder"></param>
    public void CopyMove(FileOperationMode mode, List<MediaItem> items, Folder destFolder) {
      var fop = new FileOperationDialog(App.WMain, mode) { PbProgress = { IsIndeterminate = false, Value = 0 } };
      fop.RunTask = Task.Run(() => {
        fop.LoadCts = new();
        var token = fop.LoadCts.Token;

        try {
          MediaItems.CopyMove(mode, items, destFolder, fop.Progress,
            (string srcFilePath, string destFilePath, ref string destFileName) =>
              AppCore.ShowFileOperationCollisionDialog(srcFilePath, destFilePath, fop, ref destFileName), token);
        }
        catch (Exception ex) {
          ErrorDialog.Show(ex);
        }
      }).ContinueWith(task => App.Core.RunOnUiThread(() => fop.Close()));

      fop.ShowDialog();

      if (mode == FileOperationMode.Move) {
        _model.ThumbsGrid.RemoveSelected();
        ThumbsGridReloadItems();
      }
    }

    public void Delete(MediaItem[] items) {
      if (items.Length == 0) return;
      var progress = new ProgressBarDialog(App.WMain, false, 1, "Removing Media Items from database ...");
      progress.AddEvents(items, null, _model.Delete, mi => mi.FilePath, null);
      progress.StartDialog();
    }

    public void ReapplyFilter() {
      _model.ThumbsGrid?.ReloadFilteredItems();
      App.Core.MarkUsedKeywordsAndPeople();
      ThumbsGridReloadItems();
    }

    public static bool TryWriteMetadata(MediaItem mediaItem) {
      if (mediaItem.IsOnlyInDb) return true;
      try {
        var bSuccess = WriteMetadata(mediaItem);
        if (bSuccess) return true;
        throw new Exception("Error writing metadata");
      }
      catch (Exception ex) {
        App.Core.LogError(ex, $"Metadata will be saved just in Database. {mediaItem.FilePath}");
        // set MediaItem as IsOnlyInDb to not save metadata to file, but keep them just in DB
        mediaItem.IsOnlyInDb = true;
        return false;
      }
    }

    private static bool WriteMetadata(MediaItem mi) {
      if (mi.MediaType == MediaType.Video) return true;
      var original = new FileInfo(mi.FilePath);
      var newFile = new FileInfo(mi.FilePath + "_newFile");
      var bSuccess = false;
      const BitmapCreateOptions createOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;

      using (Stream originalFileStream = File.Open(original.FullName, FileMode.Open, FileAccess.Read)) {
        var decoder = BitmapDecoder.Create(originalFileStream, createOptions, BitmapCacheOption.None);
        if (decoder.CodecInfo != null && decoder.CodecInfo.FileExtensions.Contains("jpg") && decoder.Frames[0] != null) {
          var metadata = decoder.Frames[0].Metadata == null
            ? new BitmapMetadata("jpg")
            : decoder.Frames[0].Metadata.Clone() as BitmapMetadata;

          if (metadata != null) {

            //People
            if (mi.People != null) {
              const string microsoftRegionInfo = @"/xmp/MP:RegionInfo";
              const string microsoftRegions = @"/xmp/MP:RegionInfo/MPRI:Regions";
              const string microsoftPersonDisplayName = @"/MPReg:PersonDisplayName";
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
                  if (!mi.People.Any(p => p.Title.Equals(personDisplayName.ToString()))) continue;
                  addedPeople.Add(personDisplayName.ToString());
                  peopleIdx++;
                  people.SetQuery($"{microsoftRegions}/{{ulong={peopleIdx}}}", existingPerson);
                }
              }

              //Adding new people
              foreach (var person in mi.People.Where(p => !addedPeople.Any(ap => ap.Equals(p.Title)))) {
                peopleIdx++;
                people.SetQuery($"{microsoftRegions}/{{ulong={peopleIdx}}}", new BitmapMetadata("xmpstruct"));
                people.SetQuery($"{microsoftRegions}/{{ulong={peopleIdx}}}" + microsoftPersonDisplayName, person.Title);
              }

              //Writing all people to MediaItem metadata
              var allPeople = people.GetQuery(microsoftRegionInfo);
              if (allPeople != null)
                metadata.SetQuery(microsoftRegionInfo, allPeople);
            }

            metadata.Rating = mi.Rating;
            metadata.Comment = mi.Comment ?? string.Empty;
            metadata.Keywords = new(mi.Keywords?.Select(k => k.FullPath).ToList() ?? new List<string>());
            metadata.SetQuery("System.Photo.Orientation", (ushort)mi.Orientation);

            //GeoNameId
            if (mi.GeoName == null)
              metadata.RemoveQuery(@"/xmp/GeoNames:GeoNameId");
            else
              metadata.SetQuery(@"/xmp/GeoNames:GeoNameId", mi.GeoName.Id.ToString());

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

    private static bool WriteMetadataToFile(MediaItem mi, FileInfo newFile, BitmapDecoder decoder, BitmapMetadata metadata, bool withThumbnail) {
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
          App.Core.LogError(ex, mi.FilePath);
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

    public static bool ReadMetadata(MediaItem mi, bool gpsOnly = false) {
      try {
        if (mi.MediaType == MediaType.Video) {
          App.Core.RunOnUiThread(() => {
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

              App.Db.SetModified<MediaItems>();
            }
            catch (Exception ex) {
              App.Core.LogError(ex, mi.FilePath);
            }
          });
        }
        else {
          using Stream srcFileStream = File.Open(mi.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
          var decoder = BitmapDecoder.Create(srcFileStream, BitmapCreateOptions.None, BitmapCacheOption.None);
          var frame = decoder.Frames[0];
          mi.Width = frame.PixelWidth;
          mi.Height = frame.PixelHeight;
          mi.SetThumbSize(true);

          App.Db.SetModified<MediaItems>();

          // true because only media item dimensions are required
          if (frame.Metadata is not BitmapMetadata bm) return true;

          // Lat Lng
          var tmpLat = bm.GetQuery("System.GPS.Latitude.Proxy")?.ToString();
          if (tmpLat != null) {
            var vals = tmpLat[0..^1].Split(',');
            mi.Lat = (int.Parse(vals[0]) + (double.Parse(vals[1], CultureInfo.InvariantCulture) / 60)) *
                  (tmpLat.EndsWith("S") ? -1 : 1);
          }

          var tmpLng = bm.GetQuery("System.GPS.Longitude.Proxy")?.ToString();
          if (tmpLng != null) {
            var vals = tmpLng[0..^1].Split(',');
            mi.Lng = (int.Parse(vals[0]) + (double.Parse(vals[1], CultureInfo.InvariantCulture) / 60)) *
                  (tmpLng.EndsWith("W") ? -1 : 1);
          }

          if (gpsOnly) return true;

          // People
          mi.People = null;
          const string microsoftRegions = @"/xmp/MP:RegionInfo/MPRI:Regions";
          const string microsoftPersonDisplayName = @"/MPReg:PersonDisplayName";

          if (bm.GetQuery(microsoftRegions) is BitmapMetadata regions) {
            var count = regions.Count();
            if (count > 0) {
              mi.People = new(count);
              foreach (var region in regions) {
                var personDisplayName = bm.GetQuery(microsoftRegions + region + microsoftPersonDisplayName);
                if (personDisplayName != null) {
                  var person = App.Core.People.GetPerson(personDisplayName.ToString(), true);
                  person.MediaItems.Add(mi);
                  mi.People.Add(person);
                }
              }
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
              if (mi.Keywords.SingleOrDefault(x => x.FullPath.Equals(k)) != null) continue;
              var keyword = App.Core.Keywords.GetByFullPath(k);
              if (keyword != null) {
                keyword.MediaItems.Add(mi);
                mi.Keywords.Add(keyword);
              }
            }
          }

          // GeoNameId
          var tmpGId = bm.GetQuery(@"/xmp/GeoNames:GeoNameId");
          if (!string.IsNullOrEmpty(tmpGId as string)) {
            // TODO dohledani/vytvoreni geoname
            mi.GeoName = App.Core.GeoNames.All.Cast<GeoName>().SingleOrDefault(x => x.Id == int.Parse(tmpGId.ToString()));
          }

          mi.SetThumbSize(true);
        }
      }
      catch (Exception ex) {
        App.Core.LogError(ex, mi.FilePath);

        // No imaging component suitable to complete this operation was found.
        if ((ex.InnerException as COMException)?.HResult == -2003292336)
          return false;

        mi.IsOnlyInDb = true;

        // true because only media item dimensions are required
        return true;
      }

      return true;
    }
  }
}
