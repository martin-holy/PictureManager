using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Extensions;
using PictureManager.Domain.Database;
using PictureManager.Domain.DataViews;
using PictureManager.Domain.Dialogs;
using PictureManager.Domain.HelperClasses;
using PictureManager.Domain.Models;
using PictureManager.Domain.Models.MediaItems;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Domain.ViewModels;

public class CoreVM {
  private readonly CoreM _m;
  private readonly Db _db;

  public MediaItemsViews MediaItemsViews { get; }

  public RelayCommand<FolderM> CompressImagesCommand { get; }
  public RelayCommand<FolderM> GetGeoNamesFromWebCommand { get; }
  public RelayCommand<FolderM> ImagesToVideoCommand { get; }
  public RelayCommand<FolderM> ReadGeoLocationFromFilesCommand { get; }
  public RelayCommand<FolderM> ReloadMetadataCommand { get; }
  public RelayCommand<FolderM> ResizeImagesCommand { get; }
  public RelayCommand<FolderM> RotateMediaItemsCommand { get; }
  public RelayCommand<FolderM> SaveImageMetadataToFilesCommand { get; }

  public CoreVM(CoreM coreM, Db db) {
    _m = coreM;
    _db = db;
    MediaItemsViews = Core.MediaItemsViews;

    CompressImagesCommand = new(x => CompressImages(GetActive<ImageM>(x)), AnyActive<ImageM>, null, "Compress Images");
    GetGeoNamesFromWebCommand = new(x => GetGeoNamesFromWeb(GetActive<ImageM>(x)), AnyActive<ImageM>, Res.IconLocationCheckin, "Get GeoNames from web");
    ImagesToVideoCommand = new(x => ImagesToVideo(GetActive<ImageM>(x)), AnyActive<ImageM>, null, "Images to Video");
    ReadGeoLocationFromFilesCommand = new(x => ReadGeoLocationFromFiles(GetActive<ImageM>(x)), AnyActive<ImageM>, Res.IconLocationCheckin, "Read GeoLocation from files");
    ReloadMetadataCommand = new(x => _m.MediaItems.ReloadMetadata(GetActive<RealMediaItemM>(x)), AnyActive<RealMediaItemM>, null, "Reload metadata");
    ResizeImagesCommand = new(x => ResizeImages(GetActive<ImageM>(x)), AnyActive<ImageM>, null, "Resize Images");
    RotateMediaItemsCommand = new(x => RotateMediaItems(GetActive<RealMediaItemM>(x)), AnyActive<RealMediaItemM>, null, "Rotate");
    SaveImageMetadataToFilesCommand = new(x => SaveImageMetadataToFiles(GetActive<ImageM>(x)), AnyActive<ImageM>, Res.IconSave, "Save Image metadata to files");
  }

  public bool AnyActive<T>(FolderM folder) where T : MediaItemM =>
    folder != null
    || (_m.MainWindow.IsInViewMode && _m.MediaItems.Current is T)
    || MediaItemsViews.Current?.Selected.Items.OfType<T>().Any() == true;

  public T[] GetActive<T>(FolderM folder) where T : MediaItemM =>
    folder != null
      ? folder.GetMediaItems(Keyboard.IsShiftOn()).OfType<T>().ToArray()
      : _m.MainWindow.IsInViewMode
        ? _m.MediaItems.Current is T current ? new[] { current } : Array.Empty<T>()
        : MediaItemsViews.Current?.Selected.Items.OfType<T>().ToArray() ?? Array.Empty<T>();

  public bool TryWriteMetadata(ImageM img) {
    try {
      if (!_m.MediaItems.WriteMetadata(img)) throw new("Error writing metadata");
      img.IsOnlyInDb = false;
    }
    catch (Exception ex) {
      Log.Error(ex, $"Metadata will be saved just in database. {img.FilePath}");
      img.IsOnlyInDb = true;
    }

    _db.Images.IsModified = true;
    return !img.IsOnlyInDb;
  }

  private void CompressImages(ImageM[] items) {
    Dialog.Show(new CompressDialogM(items, Core.Settings.JpegQualityLevel));
    _m.MediaItems.UpdateModifiedCount();
  }

  private void GetGeoNamesFromWeb(ImageM[] items) {
    items = items.Where(x => x.GeoLocation is { } gl && gl.GeoName == null && gl.Lat != null && gl.Lng != null).ToArray();
    if (items.Length == 0) return;
    GeoLocationProgressDialog(items, "Getting GeoNames from web ...", async mi => {
      if (_m.GeoNames.ApiLimitExceeded) return;
      _db.MediaItemGeoLocation.ItemUpdate(new(mi,
        await _db.GeoLocations.GetOrCreate(mi.GeoLocation.Lat, mi.GeoLocation.Lng, null, null)));
    });
  }

  private void GeoLocationProgressDialog(ImageM[] items, string msg, Func<ImageM, Task> action) {
    var progress = new ProgressBarSyncDialog(msg, Res.IconLocationCheckin);
    _ = progress.Init(items, null, action, mi => mi.FilePath, null);
    progress.Start();
    _db.MediaItems.RaiseMetadataChanged(items.Cast<MediaItemM>().ToArray());
  }

  private void ImagesToVideo(ImageM[] items) {
    if (items.Length == 1) return;
    Dialog.Show(new ImagesToVideoDialogM(
      items,
      (folder, fileName) => {
        var mi = _db.Videos.ItemCreate(folder, fileName);
        var mim = new MediaItemMetadata(mi);
        MediaItemsM.ReadMetadata(mim, false);
        mi.SetThumbSize();
        MediaItemsViews.Current.LoadedItems.Add(mi);
        MediaItemsViews.Current.SoftLoad(MediaItemsViews.Current.LoadedItems, true, true);
      })
    );
  }

  private void ReadGeoLocationFromFiles(ImageM[] items, bool reload = true) {
    GeoLocationProgressDialog(items, "Reading GeoLocations from files ...", async mi => {
      if (!reload && mi.GeoLocation != null) return;
      var mim = new MediaItemMetadata(mi);
      await Task.Run(() => { MediaItemsM.ReadMetadata(mim, true); });
      if (mim.Success) await mim.FindGeoLocation(false);
    });
  }

  private void ResizeImages(ImageM[] items) =>
    Dialog.Show(new ResizeImagesDialogM(items));

  private void RotateMediaItems(RealMediaItemM[] items) {
    if (RotationDialogM.Open(out var rotation))
      _db.MediaItems.Rotate(items, rotation);
  }

  private void SaveImageMetadataToFiles(ImageM[] items) {
    if (Dialog.Show(new MessageDialog(
          "Save metadata to files",
          "Do you really want to save image metadata to {0} file{1}?".Plural(items.Length),
          Res.IconQuestion,
          true)) != 1) return;

    var progress = new ProgressBarAsyncDialog("Saving metadata to files...", Res.IconImage, true, Environment.ProcessorCount);
    progress.Init(items, null, mi => TryWriteMetadata(mi), mi => mi.FilePath, null);
    progress.Start();
    Dialog.Show(progress);
    _ = _m.MediaItemsStatusBar.UpdateFileSize();
    _m.MediaItems.UpdateModifiedCount();
  }
}