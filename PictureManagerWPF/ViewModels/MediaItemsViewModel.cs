using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Properties;
using PictureManager.Utils;

namespace PictureManager.ViewModels {
  public class MediaItemsViewModel {
    public ObservableCollection<ObservableCollection<MediaItem>> SplittedItems { get; } = new ObservableCollection<ObservableCollection<MediaItem>>();

    private CancellationTokenSource _loadCts;
    private Task _loadTask;

    ~MediaItemsViewModel() {
      if (_loadCts != null) {
        _loadCts.Dispose();
        _loadCts = null;
      }
    }

    public async Task LoadAsync(List<MediaItem> mediaItems, List<Folder> folders, bool sorted = true) {
      // cancel previous work
      if (_loadCts != null) {
        _loadCts.Cancel();
        await _loadTask;
      }

      // scroll to top to fix funny loading
      if (App.Core.Model.MediaItems.FilteredItems.Count > 0)
        ScrollTo(App.Core.Model.MediaItems.FilteredItems[0]);

      // Clear before new load
      App.Core.Model.MediaItems.ClearItBeforeLoad();
      foreach (var splittedItem in SplittedItems)
        splittedItem.Clear();

      SplittedItems.Clear();
      App.WMain.ImageComparerTool.Close();

      App.Core.AppInfo.ProgressBarIsIndeterminate = true;

      _loadTask = Task.Run(async () => {
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;
        var items = new List<MediaItem>();

        if (mediaItems != null) {
          // filter out items if directory or file not exists or Viewer can not see items
          items = await MediaItems.VerifyAccessibilityOfMediaItemsAsync(mediaItems, token);
        }

        if (folders != null) {
          items = await App.Core.Model.MediaItems.GetMediaItemsFromFoldersAsync(folders, token);
        }

        if (sorted)
          items = items.OrderBy(x => x.FileName).ToList();

        // set thumb size and add Media Items to LoadedItems
        foreach (var mi in items) {
          mi.SetThumbSize();
          App.Core.Model.MediaItems.LoadedItems.Add(mi);
        }

        // filter Media Items and add them to FilteredItems
        foreach (var mi in MediaItems.Filter(App.Core.Model.MediaItems.LoadedItems)) {
          App.Core.Model.MediaItems.FilteredItems.Add(mi);
        }

        App.Core.Model.MediaItems.OnPropertyChanged(nameof(App.Core.Model.MediaItems.PositionSlashCount));
        App.Core.Model.SetMediaItemSizesLoadedRange();
        await LoadThumbnailsAsync(App.Core.Model.MediaItems.FilteredItems.ToArray(), token);
      });

      await _loadTask;
    }

    private async Task LoadThumbnailsAsync(IReadOnlyCollection<MediaItem> items, CancellationToken token) {
      App.Core.AppInfo.ProgressBarIsIndeterminate = false;
      App.Core.AppInfo.ProgressBarValueA = 0;
      App.Core.AppInfo.ProgressBarValueB = 0;

      await Task.Run(async () => {
        // read metadata for new items and add thumbnails to grid
        var metadata = ReadMetadataAndListThumbsAsync(items, token);
        // create thumbnails
        var thumbs = Imaging.CreateThumbnailsAsync(items, token);

        await Task.WhenAll(metadata, thumbs);

        var saveDb = metadata.Result;

        if (token.IsCancellationRequested) {
          saveDb = true;
          if (Application.Current.Dispatcher != null)
            await Application.Current.Dispatcher.InvokeAsync(delegate {
              Delete(App.Core.Model.MediaItems.All.Where(x => x.IsNew).ToArray());
            });
        }

        if (saveDb)
          App.Core.Model.Sdb.SaveAllTables();
      });

      // TODO: is this necessary?
      if (App.Core.Model.MediaItems.Current != null) {
        App.Core.Model.MediaItems.SetSelected(App.Core.Model.MediaItems.Current, false);
        App.Core.Model.MediaItems.SetSelected(App.Core.Model.MediaItems.Current, true);
      }

      GC.Collect();
    }

    private Task<bool> ReadMetadataAndListThumbsAsync(IReadOnlyCollection<MediaItem> items, CancellationToken token) {
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

            Application.Current.Dispatcher?.Invoke(delegate { App.Core.Model.MediaItems.MediaItemsCount++; });

            if (!ReadMetadata(mi)) {
              // delete corrupted MediaItems
              Application.Current.Dispatcher?.Invoke(delegate {
                App.Core.Model.MediaItems.LoadedItems.Remove(mi);
                App.Core.Model.MediaItems.FilteredItems.Remove(mi);
                App.Core.Model.MediaItems.Delete(mi);
                App.Core.AppInfo.ProgressBarValueA = percent;
              });

              continue;
            }

            mi.SetThumbSize();
            mediaItemsModified = true;
          }

          Application.Current.Dispatcher?.Invoke(delegate {
            mi.SetInfoBox();
            SplittedItemsAdd(mi);
            App.Core.AppInfo.ProgressBarValueA = percent;
          });
        }

        return mediaItemsModified;
      });
    }

    private void SplittedItemsAdd(MediaItem mi) {
      var lastRowIndex = SplittedItems.Count - 1;
      if (lastRowIndex == -1) {
        SplittedItems.Add(new ObservableCollection<MediaItem>());
        lastRowIndex++;
      }

      var rowMaxWidth = App.WMain.ThumbsBox.ActualWidth;
      const int itemOffset = 6; //border, margin, padding, ... //TODO find the real value

      var rowWidth = SplittedItems[lastRowIndex].Sum(x => x.ThumbWidth + itemOffset);
      if (mi.ThumbWidth <= rowMaxWidth - rowWidth) {
        SplittedItems[lastRowIndex].Add(mi);
      }
      else {
        SplittedItems.Add(new ObservableCollection<MediaItem>());
        SplittedItems[lastRowIndex + 1].Add(mi);
      }
    }

    public void SplittedItemsReload() {
      foreach (var itemsRow in SplittedItems)
        itemsRow.Clear();

      SplittedItems.Clear();
      App.WMain.UpdateLayout();

      var row = new ObservableCollection<MediaItem>();
      var rowWidth = 0;
      var rowMaxWidth = App.WMain.ThumbsBox.ActualWidth;

      const int itemOffset = 6; //border, margin, padding, ...

      foreach (var mi in App.Core.Model.MediaItems.FilteredItems) {
        if (mi.ThumbWidth + itemOffset <= rowMaxWidth - rowWidth) {
          row.Add(mi);
          rowWidth += mi.ThumbWidth + itemOffset;
        }
        else {
          SplittedItems.Add(row);
          row = new ObservableCollection<MediaItem> { mi };
          rowWidth = mi.ThumbWidth + itemOffset;
        }
      }

      SplittedItems.Add(row);
    }

    public void ScrollToCurrent() {
      if (App.Core.Model.MediaItems.Current == null)
        ScrollToTop();
      else
        ScrollTo(App.Core.Model.MediaItems.Current);
    }

    public void ScrollToTop() {
      App.WMain.ThumbsBox.FindChild<ScrollViewer>("ThumbsBoxScrollViewer").ScrollToTop();
    }

    public void ScrollTo(MediaItem mi) {
      var rowIndex = 0;
      foreach (var row in SplittedItems) {
        if (row.Any(x => x.Id.Equals(mi.Id)))
          break;
        rowIndex++;
      }

      App.WMain.ThumbsBox.FindChild<VirtualizingStackPanel>("ThumbsBoxStackPanel").BringIndexIntoViewPublic(rowIndex);
    }

    public void SetOrientation(MediaItem[] mediaItems, Rotation rotation) {
      App.Core.Model.MediaItems.Helper.IsModified = true;

      var progress = new ProgressBarDialog(App.WMain, true, Environment.ProcessorCount, "Changing orientation ...");
      progress.AddEvents(
        mediaItems,
        null,
        // action
        async delegate (MediaItem mi) {
          var newOrientation = 0;
          switch ((MediaOrientation)mi.Orientation) {
            case MediaOrientation.Rotate90: newOrientation = 90; break;
            case MediaOrientation.Rotate180: newOrientation = 180; break;
            case MediaOrientation.Rotate270: newOrientation = 270; break;
          }

          switch (rotation) {
            case Rotation.Rotate90: newOrientation += 90; break;
            case Rotation.Rotate180: newOrientation += 180; break;
            case Rotation.Rotate270: newOrientation += 270; break;
          }

          if (newOrientation >= 360) newOrientation -= 360;

          switch (newOrientation) {
            case 0: mi.Orientation = (int)MediaOrientation.Normal; break;
            case 90: mi.Orientation = (int)MediaOrientation.Rotate90; break;
            case 180: mi.Orientation = (int)MediaOrientation.Rotate180; break;
            case 270: mi.Orientation = (int)MediaOrientation.Rotate270; break;
          }

          TryWriteMetadata(mi);
          mi.SetThumbSize();
          await Imaging.CreateThumbnailAsync(mi.MediaType, mi.FilePath, mi.FilePathCache, mi.ThumbSize);
          mi.ReloadThumbnail();
        },
        mi => mi.FilePath,
        // onCompleted
        delegate {
          SplittedItemsReload();
          ScrollToCurrent();
          App.Core.Model.Sdb.SaveAllTables();
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
      var fop = new FileOperationDialog { Owner = App.WMain, PbProgress = { IsIndeterminate = false, Value = 0 } };
      fop.RunTask = Task.Run(() => {
        fop.LoadCts = new CancellationTokenSource();
        var token = fop.LoadCts.Token;

        try {
          MediaItems.CopyMove(mode, items, destFolder, fop.Progress,
            (string srcFilePath, string destFilePath, ref string destFileName) =>
              AppCore.ShowFileOperationCollisionDialog(srcFilePath, destFilePath, fop, ref destFileName), token);
        }
        catch (Exception ex) {
          ErrorDialog.Show(ex);
        }
      }).ContinueWith(task => Core.Instance.RunOnUiThread(() => fop.Close()));

      fop.ShowDialog();

      if (mode == FileOperationMode.Move) {
        App.Core.Model.MediaItems.RemoveSelected(false, null);
        SplittedItemsReload();
        ScrollToCurrent();
      }
    }

    public void Delete(MediaItem[] items) {
      var progress = new ProgressBarDialog(App.WMain, false, 1, "Removing Media Items from database ...");
      progress.AddEvents(items, null, App.Core.Model.MediaItems.Delete, mi => mi.FilePath, null);
      progress.StartDialog();
    }

    public void ReapplyFilter() {
      App.Core.Model.MediaItems.Current = null;
      App.Core.Model.MediaItems.FilteredItems.Clear();

      foreach (var mi in MediaItems.Filter(App.Core.Model.MediaItems.LoadedItems))
        App.Core.Model.MediaItems.FilteredItems.Add(mi);

      App.Core.Model.MediaItems.OnPropertyChanged(nameof(App.Core.Model.MediaItems.PositionSlashCount));
      App.Core.Model.MarkUsedKeywordsAndPeople();
      SplittedItemsReload();
      ScrollToTop();
    }

    public static bool TryWriteMetadata(MediaItem mediaItem) {
      App.Core.Model.MediaItems.Helper.IsModified = true;
      if (WriteMetadata(mediaItem)) return true;
      Imaging.ReSaveImage(mediaItem.FilePath);
      return WriteMetadata(mediaItem);
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
            metadata.Keywords = new ReadOnlyCollection<string>(mi.Keywords?.Select(k => k.FullPath).ToList() ?? new List<string>());
            metadata.SetQuery("System.Photo.Orientation", (ushort)mi.Orientation);

            //GeoNameId
            if (mi.GeoName == null)
              metadata.RemoveQuery(@"/xmp/GeoNames:GeoNameId");
            else
              metadata.SetQuery(@"/xmp/GeoNames:GeoNameId", mi.GeoName.Id.ToString());

            var encoder = new JpegBitmapEncoder { QualityLevel = Settings.Default.JpegQualityLevel };
            encoder.Frames.Add(BitmapFrame.Create(decoder.Frames[0], decoder.Frames[0].Thumbnail, metadata,
              decoder.Frames[0].ColorContexts));

            var hResult = 0;
            try {
              using (Stream newFileStream = File.Open(newFile.FullName, FileMode.Create, FileAccess.ReadWrite)) {
                encoder.Save(newFileStream);
              }
              bSuccess = true;
            }
            catch (Exception ex) {
              bSuccess = false;
              hResult = ex.HResult;
              App.Core.LogError(ex, mi.FilePath);
            }

            //There is too much metadata to be written to the bitmap. (Exception from HRESULT: 0x88982F52)
            //Problem with ThumbnailImage in JPEG images taken by Huawei P10
            if (!bSuccess && hResult == -2146233033) {
              if (metadata.ContainsQuery("/app1/thumb/"))
                metadata.RemoveQuery("/app1/thumb/");
              encoder = new JpegBitmapEncoder { QualityLevel = Settings.Default.JpegQualityLevel };
              encoder.Frames.Add(BitmapFrame.Create(decoder.Frames[0], null, metadata,
                decoder.Frames[0].ColorContexts));

              try {
                using (Stream newFileStream = File.Open(newFile.FullName, FileMode.Create, FileAccess.ReadWrite)) {
                  encoder.Save(newFileStream);
                }

                bSuccess = true;
              }
              catch (Exception ex) {
                bSuccess = false;
                App.Core.LogError(ex, mi.FilePath);
              }
            }
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

    public static bool ReadMetadata(MediaItem mi, bool gpsOnly = false) {
      try {
        if (mi.MediaType == MediaType.Video) {
          Application.Current.Dispatcher?.Invoke(delegate {
            try {
              var size = ShellStuff.FileInformation.GetVideoMetadata(mi.Folder.FullPath, mi.FileName);
              mi.Height = size[0];
              mi.Width = size[1];
              mi.Orientation = size[2];
            }
            catch (Exception ex) {
              App.Core.LogError(ex, mi.FilePath);
            }
          });
        }
        else {
          using (Stream srcFileStream = File.Open(mi.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
            var decoder = BitmapDecoder.Create(srcFileStream, BitmapCreateOptions.None, BitmapCacheOption.None);
            var frame = decoder.Frames[0];
            mi.Width = frame.PixelWidth;
            mi.Height = frame.PixelHeight;

            mi.SetThumbSize();
            App.Core.Model.MediaItems.Helper.IsModified = true;

            // true because only media item dimensions are required
            if (!(frame.Metadata is BitmapMetadata bm)) return true;

            // Lat Lng
            var tmpLat = bm.GetQuery("System.GPS.Latitude.Proxy")?.ToString();
            if (tmpLat != null) {
              var vals = tmpLat.Substring(0, tmpLat.Length - 1).Split(',');
              mi.Lat = (int.Parse(vals[0]) + double.Parse(vals[1], CultureInfo.InvariantCulture) / 60) *
                    (tmpLat.EndsWith("S") ? -1 : 1);
            }

            var tmpLng = bm.GetQuery("System.GPS.Longitude.Proxy")?.ToString();
            if (tmpLng != null) {
              var vals = tmpLng.Substring(0, tmpLng.Length - 1).Split(',');
              mi.Lng = (int.Parse(vals[0]) + double.Parse(vals[1], CultureInfo.InvariantCulture) / 60) *
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
                mi.People = new List<Person>(count);
                foreach (var region in regions) {
                  var personDisplayName = bm.GetQuery(microsoftRegions + region + microsoftPersonDisplayName);
                  if (personDisplayName != null) {
                    var person = App.Core.Model.People.GetPerson(personDisplayName.ToString(), true);
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
              mi.Keywords = new List<Keyword>();
              // Filter out duplicities
              foreach (var k in bm.Keywords.OrderByDescending(x => x)) {
                if (mi.Keywords.SingleOrDefault(x => x.FullPath.Equals(k)) != null) continue;
                var keyword = App.Core.Model.Keywords.GetByFullPath(k);
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
              App.Core.Model.GeoNames.AllDic.TryGetValue(int.Parse(tmpGId.ToString()), out var geoname);
              mi.GeoName = geoname;
            }
          }
        }
      }
      catch (Exception ex) {
        App.Core.LogError(ex, mi.FilePath);

        // No imaging component suitable to complete this operation was found.
        if ((ex.InnerException as COMException)?.HResult == -2003292336)
          return false;

        // true because only media item dimensions are required
        return true;
      }

      return true;
    }
  }
}
