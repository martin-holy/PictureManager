using MH.UI.Dialogs;
using MH.Utils.Extensions;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.MediaItem;

public sealed class ReloadMetadataDialog : ParallelProgressDialog<RealMediaItemM> {
  private readonly RealMediaItemM[] _items;
  private readonly MediaItemS _mediaItemS;

  public ReloadMetadataDialog(RealMediaItemM[] items, MediaItemS mediaItemS) :
    base("Reloading metadata...", MH.UI.Res.IconImage, items) {
    _items = items;
    _mediaItemS = mediaItemS;
    _autoRun();
  }

  protected override Task _do(RealMediaItemM item, CancellationToken token) {
    _reportProgress(item.FilePath);
    return _mediaItemS.ReloadMetadata(item);
  }

  protected override void _doAfter() =>
    _mediaItemS.OnMetadataReloaded(_items);

  public static async Task Open(RealMediaItemM[] items, MediaItemS mediaItemS) {
    if (items.Length == 0 || await ShowAsync(new MessageDialog(
          "Reload metadata from files",
          "Do you really want to reload image metadata for {0} file{1}?".Plural(items.Length),
          MH.UI.Res.IconQuestion,
          true)) != 1) return;

    await ShowAsync(new ReloadMetadataDialog(items, mediaItemS));
  }
}