using MH.UI.Dialogs;
using PictureManager.Common.Features.MediaItem.Image;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.GeoLocation;

public sealed class GetGeoNamesFromWebDialog : ProgressDialog<ImageM> {
  private readonly CoreR _coreR;

  public GetGeoNamesFromWebDialog(ImageM[] items, CoreR coreR) :
    base("Getting GeoNames from web ...", Res.IconLocationCheckin, items) {
    _coreR = coreR;
    RunSync = true;
    AutoRun();
  }

  protected override async Task Do(ImageM item, CancellationToken token) {
    ReportProgress(item.FileName);
    if (_coreR.GeoName.ApiLimitExceeded) return;
    var gl = await _coreR.GeoLocation.GetOrCreate(item.GeoLocation?.Lat, item.GeoLocation?.Lng, null, null);
    _coreR.MediaItemGeoLocation.ItemUpdate(new(item, gl));
  }

  public static bool Open(ImageM[] items, CoreR coreR) {
    items = items.Where(x => x.GeoLocation is { GeoName: null, Lat: not null, Lng: not null }).ToArray();
    if (items.Length == 0) return false;
    Show(new GetGeoNamesFromWebDialog(items, coreR));
    return true;
  }
}