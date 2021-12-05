using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using MH.UI.WPF.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Commands;
using PictureManager.Dialogs;
using PictureManager.Domain;
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

    public MediaItemsM Model { get; }
    public Dictionary<int, MediaItemBaseVM> All { get; } = new();
    public MediaItemBaseVM Current => ToViewModel(Model.Current);

    public RelayCommand<object> RotateCommand { get; }
    public RelayCommand<object> RenameCommand { get; }
    public RelayCommand<object> DeleteCommand { get; }
    public RelayCommand<object> EditCommand { get; }
    public RelayCommand<object> SaveEditCommand { get; }
    public RelayCommand<object> CancelEditCommand { get; }
    public RelayCommand<object> CommentCommand { get; }
    public RelayCommand<object> ReloadMetadataCommand { get; }
    public RelayCommand<FolderTreeVM> ReloadMetadataInFolderCommand { get; }

    public MediaItemsBaseVM(Core core, AppCore coreVM, MediaItemsM model) {
      _core = core;
      _coreVM = coreVM;
      Model = model;

      Model.PropertyChanged += (_, e) => {
        if (nameof(Model.Current).Equals(e.PropertyName))
          OnPropertyChanged(nameof(Current));
      };

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
        () => _core.ThumbnailsGridsM.Current?.SelectedItems.Count > 0 || _coreVM.AppInfo.AppMode == AppMode.Viewer);

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
      #endregion

      _core.SegmentsM.SegmentPersonChangedEvent += (_, e) => SetInfoBox(e.Segment.MediaItem);
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

    private void Rotate() {
      var rotation = RotationDialog.Show();
      if (rotation == Rotation.Rotate0) return;
      SetOrientation(_core.ThumbnailsGridsM.Current.FilteredItems.Where(x => x.IsSelected).ToArray(), rotation);

      if (_coreVM.AppInfo.AppMode != AppMode.Viewer) return;
      // TODO remove App.WMain
      App.WMain.MediaViewer.SetMediaItemSource(Current);
    }

    private async void Rename() {
      var inputDialog = new InputDialog {
        Owner = App.WMain,
        IconName = IconName.Notification,
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
        // TODO
        App.WMain.StatusPanel.OnPropertyChanged(nameof(App.WMain.StatusPanel.FilePath));
        App.WMain.StatusPanel.OnPropertyChanged(nameof(App.WMain.StatusPanel.DateAndTime));
      }
      catch (Exception ex) {
        _core.LogError(ex);
      }
    }

    public void SetInfoBox(MediaItemM mi) => ToViewModel(mi)?.SetInfoBox();

    public void Delete(MediaItemM[] items) {
      if (items.Length == 0) return;
      var progress = new ProgressBarDialog(App.WMain, false, 1, "Removing Media Items from database ...");
      progress.AddEvents(items, null, Model.Delete, mi => mi.FilePath, null);
      progress.StartDialog();
    }

    private async void Delete() {
      // TODO remove App.WMain
      var currentThumbsGrid = _core.ThumbnailsGridsM.Current;
      var items = _coreVM.AppInfo.AppMode == AppMode.Viewer
        ? new() { Model.Current }
        : currentThumbsGrid.FilteredItems.Where(x => x.IsSelected).ToList();
      var count = items.Count;

      if (!MessageDialog.Show("Delete Confirmation",
        $"Do you really want to delete {count} item{(count > 1 ? "s" : string.Empty)}?", true)) return;

      Model.Current = MediaItemsM.GetNewCurrent(currentThumbsGrid != null
          ? currentThumbsGrid.LoadedItems
          : App.WMain.MediaViewer.MediaItems.Select(x => x.Model).ToList(),
        items);

      Model.Delete(items, AppCore.FileOperationDelete);
      await _coreVM.ThumbnailsGridsVM.ThumbsGridReloadItems();

      if (_coreVM.MainTabsVM.Selected is SegmentMatchingControl smc)
        _ = smc.SortAndReload();

      if (_coreVM.AppInfo.AppMode == AppMode.Viewer) {
        _ = App.WMain.MediaViewer.MediaItems.Remove(ToViewModel(items[0]));
        if (Model.Current != null)
          App.WMain.MediaViewer.SetMediaItemSource(ToViewModel(Model.Current));
        else
          WindowCommands.SwitchToBrowser();
      }
    }

    public void SetMetadata(ICatTreeViewTagItem item) {
      foreach (var mi in _core.ThumbnailsGridsM.Current.SelectedItems) {
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
        (o, e) => _ = _coreVM.ThumbnailsGridsVM.ThumbsGridReloadItems());

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
        _core.ThumbnailsGridsM.Current.RemoveSelected();
        _ = _coreVM.ThumbnailsGridsVM.ThumbsGridReloadItems();
      }
    }

    public void SaveEdit() {
      var progress = new ProgressBarDialog(App.WMain, true, Environment.ProcessorCount, "Saving metadata ...");
      progress.AddEvents(
        Model.ModifiedItems.ToArray(),
        null,
        // action
        async mi => {
          TryWriteMetadata(mi);
          await _core.RunOnUiThread(() => Model.SetModified(mi, false));
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

      progress.StartDialog();
    }

    private void CancelEdit() {
      var progress = new ProgressBarDialog(App.WMain, false, Environment.ProcessorCount, "Reloading metadata ...");
      progress.AddEvents(
        Model.ModifiedItems.ToArray(),
        null,
        // action
        async mi => {
          await ReadMetadata(mi);

          await _core.RunOnUiThread(() => {
            Model.SetModified(mi, false);
            SetInfoBox(mi);
          });
        },
        mi => mi.FilePath,
        // onCompleted
        (o, e) => {
          _coreVM.MarkUsedKeywordsAndPeople();
          Model.IsEditModeOn = false;
        });

      progress.StartDialog();
    }

    private void Comment() {
      var inputDialog = new InputDialog {
        Owner = App.WMain,
        IconName = IconName.Notification,
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
      SetInfoBox(Model.Current);
      Model.Current.OnPropertyChanged(nameof(Model.Current.Comment));
      TryWriteMetadata(Model.Current);
      Model.DataAdapter.IsModified = true;
    }

    private void ReloadMetadataInFolder(FolderTreeVM folder) {
      var recursive = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
      ReloadMetadata(folder.Model.GetMediaItems(recursive), true);
    }

    private void ReloadMetadata(List<MediaItemM> mediaItems, bool updateInfoBox = false) {
      var progress = new ProgressBarDialog(App.WMain, true, Environment.ProcessorCount, "Reloading metadata ...");
      progress.AddEvents(
        mediaItems.ToArray(),
        null,
        // action
        async (mi) => {
          await ReadMetadata(mi);

          // set info box just for loaded media items
          if (updateInfoBox)
            await _core.RunOnUiThread(() => SetInfoBox(mi));
        },
        mi => mi.FilePath,
        // onCompleted
        (_, _) => _coreVM.MarkUsedKeywordsAndPeople());

      progress.Start();
    }
  }
}
