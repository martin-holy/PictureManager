using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Extensions;
using PictureManager.Domain.DataViews;
using PictureManager.Domain.Dialogs;
using PictureManager.Domain.HelperClasses;
using PictureManager.Domain.Models;
using PictureManager.Domain.Models.MediaItems;
using PictureManager.Domain.Repositories;
using PictureManager.Domain.Services;
using PictureManager.Domain.ViewModels.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Domain.ViewModels;

public class CoreVM {
  private readonly CoreS _coreS;
  private readonly CoreR _coreR;

  public GeoNameVM GeoName { get; }
  public MediaItemVM MediaItem { get; }
  public SegmentVM Segment { get; }

  public ImageComparerVM ImageComparer { get; } = new();
  public MainWindowVM MainWindow { get; } = new();
  public MediaItemsViews MediaItemsViews { get; }
  public MediaViewerVM MediaViewer { get; } = new();
  public SegmentsDrawerVM SegmentsDrawer { get; }
  public TitleProgressBarVM TitleProgressBar { get; } = new();

  public static RelayCommand AppClosingCommand { get; set; }
  public static RelayCommand OpenAboutCommand { get; } = new(() => Dialog.Show(new AboutDialogM()), null, "About");
  public static RelayCommand OpenLogCommand { get; } = new(() => Dialog.Show(new LogDialogM()), null, "Open log");
  public static RelayCommand OpenSettingsCommand { get; set; }
  public static RelayCommand SaveDbCommand { get; set; }
  public static RelayCommand<FolderM> CompressImagesCommand { get; set; }
  public static RelayCommand<FolderM> GetGeoNamesFromWebCommand { get; set; }
  public static RelayCommand<FolderM> ImagesToVideoCommand { get; set; }
  public static RelayCommand<FolderM> ReadGeoLocationFromFilesCommand { get; set; }
  public static RelayCommand<FolderM> ReloadMetadataCommand { get; set; }
  public static RelayCommand<FolderM> ResizeImagesCommand { get; set; }
  public static RelayCommand<FolderM> RotateMediaItemsCommand { get; set; }
  public static RelayCommand<FolderM> SaveImageMetadataToFilesCommand { get; set; }

  public CoreVM(CoreS coreS, CoreR coreR) {
    _coreS = coreS;
    _coreR = coreR;

    GeoName = new(_coreR.GeoName);
    MediaItem = new(this, _coreS.MediaItem);
    Segment = new(_coreS.Segment, _coreR.Segment);

    MediaItemsViews = Core.MediaItemsViews;
    SegmentsDrawer = new(_coreS.Segment, _coreR.Segment.Drawer);

    UpdateMediaItemsCount();

    AppClosingCommand = new(AppClosing);
    OpenSettingsCommand = new(OpenSettings, Res.IconSettings, "Settings");
    SaveDbCommand = new(() => _coreR.SaveAllTables(), () => _coreR.Changes > 0, Res.IconDatabase, "Save changes");
    CompressImagesCommand = new(x => CompressImages(GetActive<ImageM>(x)), AnyActive<ImageM>, null, "Compress Images");
    GetGeoNamesFromWebCommand = new(x => GetGeoNamesFromWeb(GetActive<ImageM>(x)), AnyActive<ImageM>, Res.IconLocationCheckin, "Get GeoNames from web");
    ImagesToVideoCommand = new(x => ImagesToVideo(GetActive<ImageM>(x)), AnyActive<ImageM>, null, "Images to Video");
    ReadGeoLocationFromFilesCommand = new(x => ReadGeoLocationFromFiles(GetActive<ImageM>(x)), AnyActive<ImageM>, Res.IconLocationCheckin, "Read GeoLocation from files");
    ReloadMetadataCommand = new(x => MediaItem.ReloadMetadata(GetActive<RealMediaItemM>(x)), AnyActive<RealMediaItemM>, null, "Reload metadata");
    ResizeImagesCommand = new(x => ResizeImages(GetActive<ImageM>(x)), AnyActive<ImageM>, null, "Resize Images");
    RotateMediaItemsCommand = new(x => RotateMediaItems(GetActive<RealMediaItemM>(x)), AnyActive<RealMediaItemM>, null, "Rotate");
    SaveImageMetadataToFilesCommand = new(x => SaveImageMetadataToFiles(GetActive<ImageM>(x)), AnyActive<ImageM>, Res.IconSave, "Save Image metadata to files");
  }

  public bool AnyActive<T>(FolderM folder = null) where T : MediaItemM =>
    folder != null
    || (MainWindow.IsInViewMode && MediaItem.Current is T)
    || MediaItemsViews.Current?.Selected.Items.OfType<T>().Any() == true;

  public T[] GetActive<T>(FolderM folder = null) where T : MediaItemM =>
    folder != null
      ? folder.GetMediaItems(Keyboard.IsShiftOn()).OfType<T>().ToArray()
      : MainWindow.IsInViewMode
        ? MediaItem.Current is T current ? new[] { current } : Array.Empty<T>()
        : MediaItemsViews.Current?.Selected.Items.OfType<T>().ToArray() ?? Array.Empty<T>();

  private void AppClosing() {
    if (_coreR.Changes > 0 &&
        Dialog.Show(new MessageDialog(
          "Database changes",
          "There are some changes in database. Do you want to save them?",
          Res.IconQuestion,
          true)) == 1)
      _coreR.SaveAllTables();

    _coreR.BackUp();
  }

  private static void OpenSettings() =>
    Core.MainTabs.Activate(Res.IconSettings, "Settings", Core.Settings);

  private void CompressImages(ImageM[] items) {
    Dialog.Show(new CompressDialogM(items, Core.Settings.JpegQualityLevel));
    UpdateModifiedMediaItemsCount();
  }

  private void GetGeoNamesFromWeb(ImageM[] items) {
    items = items.Where(x => x.GeoLocation is { } gl && gl.GeoName == null && gl.Lat != null && gl.Lng != null).ToArray();
    if (items.Length == 0) return;
    GeoLocationProgressDialog(items, "Getting GeoNames from web ...", async mi => {
      if (_coreR.GeoName.ApiLimitExceeded) return;
      _coreR.MediaItemGeoLocation.ItemUpdate(new(mi,
        await _coreR.GeoLocation.GetOrCreate(mi.GeoLocation.Lat, mi.GeoLocation.Lng, null, null)));
    });
  }

  private void GeoLocationProgressDialog(ImageM[] items, string msg, Func<ImageM, Task> action) {
    var progress = new ProgressBarSyncDialog(msg, Res.IconLocationCheckin);
    _ = progress.Init(items, null, action, mi => mi.FilePath, null);
    progress.Start();
    _coreR.MediaItem.RaiseMetadataChanged(items.Cast<MediaItemM>().ToArray());
  }

  private void ImagesToVideo(ImageM[] items) {
    if (items.Length == 1) return;
    Dialog.Show(new ImagesToVideoDialogM(
      items,
      (folder, fileName) => {
        var mi = _coreR.Video.ItemCreate(folder, fileName);
        var mim = new MediaItemMetadata(mi);
        MediaItemS.ReadMetadata(mim, false);
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
      await Task.Run(() => { MediaItemS.ReadMetadata(mim, true); });
      if (mim.Success) await mim.FindGeoLocation(false);
    });
  }

  private void ResizeImages(ImageM[] items) =>
    Dialog.Show(new ResizeImagesDialogM(items));

  private void RotateMediaItems(RealMediaItemM[] items) {
    if (RotationDialogM.Open(out var rotation))
      _coreR.MediaItem.Rotate(items, rotation);
  }

  private void SaveImageMetadataToFiles(ImageM[] items) {
    if (Dialog.Show(new MessageDialog(
          "Save metadata to files",
          "Do you really want to save image metadata to {0} file{1}?".Plural(items.Length),
          Res.IconQuestion,
          true)) != 1) return;

    var progress = new ProgressBarAsyncDialog("Saving metadata to files...", Res.IconImage, true, Environment.ProcessorCount);
    progress.Init(items, null, mi => _coreS.Image.TryWriteMetadata(mi), mi => mi.FilePath, null);
    progress.Start();
    Dialog.Show(progress);
    _ = MainWindow.StatusBar.UpdateFileSize();
    UpdateModifiedMediaItemsCount();
  }

  public void UpdateMediaItemsCount() =>
    MediaItem.ItemsCount = _coreR.Image.All.Count + _coreR.Video.All.Count;

  public void UpdateModifiedMediaItemsCount() =>
    MediaItem.ModifiedItemsCount =
      _coreR.Image.All.Count(x => x.IsOnlyInDb) +
      _coreR.Video.All.Count(x => x.IsOnlyInDb);
}