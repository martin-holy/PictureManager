using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using MH.UI.WPF.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Extensions;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Domain.Utils;
using PictureManager.Properties;
using PictureManager.Utils;
using PictureManager.ViewModels.Tree;
using ObservableObject = MH.Utils.BaseClasses.ObservableObject;

namespace PictureManager.ViewModels {
  public class MediaItemsVM : ObservableObject {
    private readonly Core _core;
    private readonly AppCore _coreVM;

    public MediaItemsM Model { get; }

    public RelayCommand<object> RotateCommand { get; }
    public RelayCommand<object> RenameCommand { get; }
    public RelayCommand<object> DeleteCommand { get; }
    public RelayCommand<object> EditCommand { get; }
    public RelayCommand<object> SaveEditCommand { get; }
    public RelayCommand<object> CancelEditCommand { get; }
    public RelayCommand<object> CommentCommand { get; }
    public RelayCommand<object> ReloadMetadataCommand { get; }
    public RelayCommand<FolderTreeVM> ReloadMetadataInFolderCommand { get; }
    public RelayCommand<object> RebuildThumbnailsCommand { get; }
    public RelayCommand<object> CompareCommand { get; }
    public RelayCommand<object> AddGeoNamesFromFilesCommand { get; }

    public MediaItemsVM(Core core, AppCore coreVM, MediaItemsM model) {
      _core = core;
      _coreVM = coreVM;
      Model = model;

      Model.WriteMetadata = WriteMetadata;

      #region Commands
      RotateCommand = new(
        Rotate,
        () => _core.ThumbnailsGridsM.Current?.FilteredItems.Count(
          x => x.IsSelected && x.MediaType == MediaType.Image) > 0);

      RenameCommand = new(
        Rename,
        () => Model.Current != null);

      DeleteCommand = new(
        Delete,
        () => _core.ThumbnailsGridsM.Current?.SelectedItems.Count > 0 || Model.Current != null);

      EditCommand = new(
        () => Model.IsEditModeOn = true,
        () => _core.ThumbnailsGridsM.Current?.FilteredItems.Count > 0);

      SaveEditCommand = new(
        SaveEdit,
        () => Model.IsEditModeOn && Model.ModifiedItems.Count > 0);

      CancelEditCommand = new(
        CancelEdit,
        () => Model.IsEditModeOn);

      CommentCommand = new(
        Comment,
        () => Model.Current != null);

      ReloadMetadataCommand = new(
        () => ReloadMetadata(_core.ThumbnailsGridsM.Current.GetSelectedOrAll()),
        () => _core.ThumbnailsGridsM.Current?.FilteredItems.Count > 0);

      ReloadMetadataInFolderCommand = new(
        ReloadMetadataInFolder,
        x => x != null);

      RebuildThumbnailsCommand = new(
        RebuildThumbnails,
        x => x is FolderM || _core.ThumbnailsGridsM.Current?.FilteredItems.Count > 0);

      CompareCommand = new(
        Compare,
        () => _core.ThumbnailsGridsM.Current?.FilteredItems.Count > 0);

      AddGeoNamesFromFilesCommand = new(
        AddGeoNamesFromFiles,
        () => _core.ThumbnailsGridsM.Current?.FilteredItems.Count(x => x.IsSelected) > 0);
      #endregion
    }

    private void Rotate() {
      var rotation = RotationDialog.Show();
      if (rotation == Rotation.Rotate0) return;
      SetOrientation(_core.ThumbnailsGridsM.Current.FilteredItems.Where(x => x.IsSelected).ToArray(), rotation);

      if (_coreVM.MediaViewerVM.IsVisible)
        _coreVM.MediaViewerVM.SetMediaItemSource(Model.Current);
    }

    private async void Rename() {
      var inputDialog = new InputDialog {
        Owner = Application.Current.MainWindow,
        IconName = "IconNotification",
        Title = "Rename",
        Question = "Add a new name.",
        Answer = Path.GetFileNameWithoutExtension(Model.Current.FileName)
      };

      inputDialog.BtnDialogOk.Click += delegate {
        var newFileName = inputDialog.TxtAnswer.Text + Path.GetExtension(Model.Current.FileName);

        if (Path.GetInvalidFileNameChars().Any(x => newFileName.IndexOf(x) != -1)) {
          inputDialog.ShowErrorMessage("New file name contains invalid character!");
          return;
        }

        if (File.Exists(IOExtensions.PathCombine(Model.Current.Folder.FullPath, newFileName))) {
          inputDialog.ShowErrorMessage("New file name already exists!");
          return;
        }

        inputDialog.DialogResult = true;
      };

      inputDialog.TxtAnswer.SelectAll();

      if (!(inputDialog.ShowDialog() ?? true)) return;

      try {
        Model.Rename(Model.Current, inputDialog.TxtAnswer.Text + Path.GetExtension(Model.Current.FileName));
        _core.ThumbnailsGridsM.Current?.FilteredItemsSetInPlace(Model.Current);
        await _coreVM.ThumbnailsGridsVM.ThumbsGridReloadItems();
        _coreVM.StatusPanelVM.CurrentMediaItemM = Model.Current;
      }
      catch (Exception ex) {
        _core.LogError(ex);
      }
    }

    private async void Delete() {
      var currentThumbsGrid = _core.ThumbnailsGridsM.Current;
      var items = _coreVM.MediaViewerVM.IsVisible
        ? new() { Model.Current }
        : currentThumbsGrid.FilteredItems.Where(x => x.IsSelected).ToList();
      var count = items.Count;

      if (Core.MessageDialogShow(
        "Delete Confirmation",
        $"Do you really want to delete {count} item{(count > 1 ? "s" : string.Empty)}?",
        "IconQuestion",
        true) != 0) return;

      Model.Current = MediaItemsM.GetNewCurrent(currentThumbsGrid != null
          ? currentThumbsGrid.LoadedItems
          : _coreVM.MediaViewerVM.MediaItems,
        items);

      Model.Delete(items, AppCore.FileOperationDelete);
      await _coreVM.ThumbnailsGridsVM.ThumbsGridReloadItems();

      // TODO do it in event
      if (_coreVM.MainTabsVM.Selected?.Content is SegmentsVM)
        _core.SegmentsM.Reload();

      if (_coreVM.MediaViewerVM.IsVisible) {
        _ = _coreVM.MediaViewerVM.MediaItems.Remove(items[0]);
        if (Model.Current != null)
          _coreVM.MediaViewerVM.SetMediaItemSource(Model.Current);
        else
          _coreVM.MainWindowVM.IsFullScreen = false;
      }
    }

    public int SetMetadata(object item) {
      var count = 0;

      foreach (var mi in _core.ThumbnailsGridsM.Current.SelectedItems) {
        var modified = true;

        switch (item) {
          case PersonTreeVM p:
            mi.People = ListExtensions.Toggle(mi.People, p.Model, true);
            break;

          case KeywordTreeVM k:
            mi.Keywords = KeywordsM.Toggle(mi.Keywords, k.Model);
            break;

          case RatingTreeVM r:
            mi.Rating = r.Value;
            break;

          case GeoNameTreeVM g:
            mi.GeoName = g.Model;
            break;

          default:
            modified = false;
            break;
        }

        if (!modified) continue;

        Model.SetModified(mi, true);
        mi.SetInfoBox();
        count++;
      }

      return count;
    }

    public async Task<bool> ReadMetadata(MediaItemM mi, bool gpsOnly = false) {
      try {
        if (mi.MediaType == MediaType.Video) {
          await Core.RunOnUiThread(() => ReadVideoMetadata(mi));
        }
        else {
          using Stream srcFileStream = File.Open(mi.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
          var decoder = BitmapDecoder.Create(srcFileStream, BitmapCreateOptions.None, BitmapCacheOption.None);
          var frame = decoder.Frames[0];
          mi.Width = frame.PixelWidth;
          mi.Height = frame.PixelHeight;
          mi.SetThumbSize(true);

          if (!mi.IsNew)
            Model.DataAdapter.IsModified = true;

          // true because only media item dimensions are required
          if (frame.Metadata is not BitmapMetadata bm) return true;

          ReadImageMetadata(mi, bm, gpsOnly);

          mi.SetThumbSize(true);
        }

        mi.IsOnlyInDb = false;
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
            mi.People.Add(await Core.RunOnUiThread(() =>
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
          await Core.RunOnUiThread(() => {
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

    public void SetOrientation(MediaItemM[] mediaItems, Rotation rotation) {
      var progress = new ProgressBarDialog("Changing orientation ...", true, Environment.ProcessorCount);
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

          Model.TryWriteMetadata(mi);
          mi.SetThumbSize(true);
          File.Delete(mi.FilePathCache);
          await Core.RunOnUiThread(() => Model.DataAdapter.IsModified = true);
        },
        mi => mi.FilePath,
        // onCompleted
        (o, e) => _ = _coreVM.ThumbnailsGridsVM.ThumbsGridReloadItems());

      progress.Start();
      Core.ProgressBarDialogShow(progress);
    }

    private static bool WriteMetadata(MediaItemM mi) {
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

    private static bool WriteMetadataToFile(MediaItemM mi, FileInfo newFile, BitmapDecoder decoder, BitmapMetadata metadata, bool withThumbnail) {
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
          Core.Instance.LogError(ex, mi.FilePath);
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
      var fop = new FileOperationDialog(Application.Current.MainWindow, mode) { PbProgress = { IsIndeterminate = false, Value = 0 } };
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
      }).ContinueWith(_ => Core.RunOnUiThread(() => fop.Close()));

      _ = fop.ShowDialog();

      if (mode == FileOperationMode.Move) {
        _core.ThumbnailsGridsM.Current.RemoveSelected();
        _ = _coreVM.ThumbnailsGridsVM.ThumbsGridReloadItems();
      }
    }

    public void SaveEdit() {
      var progress = new ProgressBarDialog("Saving metadata ...", true, Environment.ProcessorCount);
      progress.AddEvents(
        Model.ModifiedItems.ToArray(),
        null,
        // action
        async mi => {
          Model.TryWriteMetadata(mi);
          await Core.RunOnUiThread(() => Model.SetModified(mi, false));
        },
        mi => mi.FilePath,
        // onCompleted
        (o, e) => {
          if (e.Cancelled)
            CancelEdit();
          else
            Model.IsEditModeOn = false;

          // TODO changing current on MediaItemsM should change current in ThumbnailsGridsM
          _core.ThumbnailsGridsM.Current.OnPropertyChanged(nameof(_core.ThumbnailsGridsM.Current.ActiveFileSize));
        });

      progress.Start();
      Core.ProgressBarDialogShow(progress);
    }

    private void CancelEdit() {
      var progress = new ProgressBarDialog("Reloading metadata ...", false, Environment.ProcessorCount);
      progress.AddEvents(
        Model.ModifiedItems.ToArray(),
        null,
        // action
        async mi => {
          await ReadMetadata(mi);

          await Core.RunOnUiThread(() => {
            Model.SetModified(mi, false);
            mi.SetInfoBox();
          });
        },
        mi => mi.FilePath,
        // onCompleted
        (o, e) => {
          _coreVM.TreeViewCategoriesVM.MarkUsedKeywordsAndPeople();
          Model.IsEditModeOn = false;
        });

      progress.Start();
      Core.ProgressBarDialogShow(progress);
    }

    private void Comment() {
      var inputDialog = new InputDialog {
        Owner = App.MainWindowV,
        IconName = "IconNotification",
        Title = "Comment",
        Question = "Add a comment.",
        Answer = Model.Current.Comment
      };

      inputDialog.BtnDialogOk.Click += delegate {
        if (inputDialog.TxtAnswer.Text.Length > 256) {
          inputDialog.ShowErrorMessage("Comment is too long!");
          return;
        }

        inputDialog.DialogResult = true;
      };

      inputDialog.TxtAnswer.SelectAll();

      if (!(inputDialog.ShowDialog() ?? true)) return;
      Model.Current.Comment = StringUtils.NormalizeComment(inputDialog.TxtAnswer.Text);
      Model.Current.SetInfoBox();
      Model.Current.OnPropertyChanged(nameof(Model.Current.Comment));
      Model.TryWriteMetadata(Model.Current);
      Model.DataAdapter.IsModified = true;
    }

    private void ReloadMetadataInFolder(FolderTreeVM folder) {
      var recursive = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
      ReloadMetadata(folder.Model.GetMediaItems(recursive), true);
    }

    private void ReloadMetadata(List<MediaItemM> mediaItems, bool updateInfoBox = false) {
      var progress = new ProgressBarDialog("Reloading metadata ...", true, Environment.ProcessorCount);
      progress.AddEvents(
        mediaItems.ToArray(),
        null,
        // action
        async (mi) => {
          await ReadMetadata(mi);

          // set info box just for loaded media items
          if (updateInfoBox)
            await Core.RunOnUiThread(mi.SetInfoBox);
        },
        mi => mi.FilePath,
        // onCompleted
        (_, _) => _coreVM.TreeViewCategoriesVM.MarkUsedKeywordsAndPeople());

      progress.Start();
      Core.ProgressBarDialogShow(progress);
    }

    private void RebuildThumbnails(object parameter) {
      var recursive = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
      var mediaItems = parameter switch {
        FolderM folder => folder.GetMediaItems(recursive),
        List<MediaItemM> items => items,
        _ => _core.ThumbnailsGridsM.Current.GetSelectedOrAll(),
      };

      var progress = new ProgressBarDialog("Rebuilding thumbnails ...", true, Environment.ProcessorCount);
      progress.AddEvents(
        mediaItems.ToArray(),
        null,
        (mi) => {
          mi.SetThumbSize(true);
          File.Delete(mi.FilePathCache);
        },
        mi => mi.FilePath,
        delegate {
          _ = _coreVM.ThumbnailsGridsVM.ThumbsGridReloadItems();
        });

      progress.Start();
      Core.ProgressBarDialogShow(progress);
    }

    private void Compare() {
      // TODO
      App.MainWindowV.ImageComparerTool.Visibility = Visibility.Visible;
      App.MainWindowV.UpdateLayout();
      App.MainWindowV.ImageComparerTool.SelectDefaultMethod();
      _ = App.MainWindowV.ImageComparerTool.Compare();
    }

    private void AddGeoNamesFromFiles() {
      if (!GeoNamesM.IsGeoNamesUserNameInSettings(Settings.Default.GeoNamesUserName)) return;

      var progress = new ProgressBarDialog("Adding GeoNames ...", true, 1);
      progress.AddEvents(
        _core.ThumbnailsGridsM.Current.FilteredItems.Where(x => x.IsSelected).ToArray(),
        null,
        // action
        async mi => {
          if (mi.Lat == null || mi.Lng == null) _ = await ReadMetadata(mi, true);
          if (mi.Lat == null || mi.Lng == null) return;

          var lastGeoName = _core.GeoNamesM.InsertGeoNameHierarchy((double)mi.Lat, (double)mi.Lng, Settings.Default.GeoNamesUserName);
          if (lastGeoName == null) return;

          mi.GeoName = lastGeoName;
          Model.TryWriteMetadata(mi);
          await Core.RunOnUiThread(() => {
            mi.SetInfoBox();
            Model.DataAdapter.IsModified = true;
          });
        },
        mi => mi.FilePath,
        // onCompleted
        delegate {
          Model.Current?.GeoName?.OnPropertyChanged(nameof(Model.Current.GeoName.FullName));
        });

      progress.Start();
      Core.ProgressBarDialogShow(progress);
    }
  }
}
