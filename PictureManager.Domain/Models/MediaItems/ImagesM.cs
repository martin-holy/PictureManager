using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Extensions;
using PictureManager.Domain.Database;
using PictureManager.Domain.DataViews;
using PictureManager.Domain.Dialogs;
using PictureManager.Domain.HelperClasses;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Domain.Models.MediaItems;

public sealed class ImagesM {
  private readonly ImagesDA _da;

  public RelayCommand<object> ReadGeoLocationFromFilesCommand { get; private set; }
  public RelayCommand<object> GetGeoNamesFromWebCommand { get; private set; }
  public RelayCommand<object> CompressCommand { get; private set; }
  public RelayCommand<MediaItemsView> ImagesToVideoCommand { get; private set; }
  public RelayCommand<object> ResizeImagesCommand { get; private set; }
  public RelayCommand<object> SaveToFilesCommand { get; private set; }

  public ImagesM(ImagesDA da) {
    _da = da;
  }

  public void InitCommands() {
    ReadGeoLocationFromFilesCommand = new(
      x => ReadGeoLocationFromFiles(Core.MediaItemsM.GetActive<ImageM>(x)),
      Core.MediaItemsM.AnyActive<ImageM>);

    GetGeoNamesFromWebCommand = new(
      x => GetGeoNamesFromWeb(Core.MediaItemsM.GetActive<ImageM>(x)),
      Core.MediaItemsM.AnyActive<ImageM>);

    CompressCommand = new(Compress, () => Core.MediaItemsM.GetActive().OfType<ImageM>().Any());

    ImagesToVideoCommand = new(
      ImagesToVideo,
      view => view?.Selected.Items.OfType<ImageM>().Any() == true);

    ResizeImagesCommand = new(
      () => Dialog.Show(new ResizeImagesDialogM(Core.MediaItemsViews.Current.GetSelectedOrAll().OfType<ImageM>().ToArray())),
      () => Core.MediaItemsViews.Current?.FilteredItems.OfType<ImageM>().Any() == true);

    SaveToFilesCommand = new(SaveToFiles);
  }

  private void ReadGeoLocationFromFiles(ImageM[] items, bool reload = true) {
    GeoLocationProgressDialog(items, "Reading GeoLocations from files ...", async mi => {
      if (!reload && mi.GeoLocation != null) return;
      var mim = new MediaItemMetadata(mi);
      await Task.Run(() => { MediaItemsM.ReadMetadata(mim, true); });
      if (mim.Success) await mim.FindGeoLocation(false);
    });
  }

  private void GetGeoNamesFromWeb(ImageM[] items) {
    items = items.Where(x => x.GeoLocation is { } gl && gl.GeoName == null && gl.Lat != null && gl.Lng != null).ToArray();
    if (items.Length == 0) return;
    GeoLocationProgressDialog(items, "Getting GeoNames from web ...", async mi => {
      if (Core.GeoNamesM.ApiLimitExceeded) return;
      Core.Db.MediaItemGeoLocation.ItemUpdate(new(mi,
        await Core.Db.GeoLocations.GetOrCreate(mi.GeoLocation.Lat, mi.GeoLocation.Lng, null, null)));
    });
  }

  private void GeoLocationProgressDialog(ImageM[] items, string msg, Func<ImageM, Task> action) {
    var progress = new ProgressBarSyncDialog(msg, Res.IconLocationCheckin, true);
    _ = progress.Init(items, null, action, mi => mi.FilePath, null);
    progress.Start();
    Core.Db.MediaItems.RaiseMetadataChanged(items.Cast<MediaItemM>().ToArray());
  }

  private void Compress() {
    Dialog.Show(new CompressDialogM(
      Core.MediaItemsM.GetActive().OfType<ImageM>().ToList(),
      Core.Settings.JpegQualityLevel));

    Core.MediaItemsM.UpdateModifiedCount();
  }

  private void ImagesToVideo(MediaItemsView view) {
    Dialog.Show(new ImagesToVideoDialogM(
      view.Selected.Items.OfType<ImageM>().ToArray(),
      (folder, fileName) => {
        var mi = _da.ItemCreate(folder, fileName);
        var mim = new MediaItemMetadata(mi);
        MediaItemsM.ReadMetadata(mim, false);
        mi.SetThumbSize();
        view.LoadedItems.Add(mi);
        view.SoftLoad(view.LoadedItems, true, true);
      })
    );
  }

  public void SaveToFiles() {
    var items = Core.MediaItemsViews.Current?.Selected.Items.OfType<ImageM>().ToArray();
    if (items == null || !items.Any()) return;
    if (Dialog.Show(new MessageDialog(
          "Save metadata to files",
          "Do you really want to save image metadata to {0} file{1}?".Plural(items.Length),
          Res.IconQuestion,
          true)) != 1) return;

    var progress = new ProgressBarAsyncDialog("Saving metadata to files...", Res.IconImage, true, Environment.ProcessorCount);
    progress.Init(items, null, mi => TryWriteMetadata(mi), mi => mi.FilePath, null);
    progress.Start();
    Dialog.Show(progress);
    Core.MediaItemsStatusBarM.OnPropertyChanged(nameof(Core.MediaItemsStatusBarM.FileSize));
    Core.MediaItemsM.UpdateModifiedCount();
  }

  public bool TryWriteMetadata(ImageM img) {
    try {
      var geoName = img.GeoLocation?.GeoName;
      if (!Core.MediaItemsM.WriteMetadata(img, geoName)) throw new("Error writing metadata");
      img.IsOnlyInDb = false;
    }
    catch (Exception ex) {
      Log.Error(ex, $"Metadata will be saved just in database. {img.FilePath}");
      img.IsOnlyInDb = true;
    }

    _da.IsModified = true;
    return !img.IsOnlyInDb;
  }
}