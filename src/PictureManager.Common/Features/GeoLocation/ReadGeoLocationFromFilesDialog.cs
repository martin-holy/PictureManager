using MH.UI.Dialogs;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.MediaItem.Image;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.GeoLocation;

public sealed class ReadGeoLocationFromFilesDialog : ProgressDialog<ImageM> {
  public ReadGeoLocationFromFilesDialog(ImageM[] items) :
    base("Reading GeoLocations from files ...", Res.IconLocationCheckin, items) {
    RunSync = true;
    _autoRun();
  }

  protected override Task _do(ImageM item, CancellationToken token) {
    _reportProgress(item.FileName);
    var mim = new MediaItemMetadata(item);
    MediaItemS.ReadMetadata(mim, true);
    return mim.Success ? mim.FindGeoLocation(false) : Task.CompletedTask;
  }
}