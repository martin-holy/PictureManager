using MH.UI.Dialogs;
using MH.Utils.Extensions;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.MediaItem;

public sealed class ReloadMetadataDialog : ParallelProgressDialog<RealMediaItemM> {
  private readonly RealMediaItemM[] _items;
  private readonly MediaItemS _mediaItemS;

  public ReloadMetadataDialog(RealMediaItemM[] items, MediaItemS mediaItemS) :
    base("Reloading metadata...", MH.UI.Res.IconImage, items, null, null) {
    _items = items;
    _mediaItemS = mediaItemS;
    AutoRun();
  }

  protected override Task Do(RealMediaItemM item) {
    ReportProgress(item.FilePath);
    return _mediaItemS.ReloadMetadata(item);
  }

  protected override void DoAfter() =>
    _mediaItemS.OnMetadataReloaded(_items);

  public static void Open(RealMediaItemM[] items, MediaItemS mediaItemS) {
    if (items.Length == 0 || Show(new MessageDialog(
          "Reload metadata from files",
          "Do you really want to reload image metadata for {0} file{1}?".Plural(items.Length),
          MH.UI.Res.IconQuestion,
          true)) != 1) return;

    Show(new ReloadMetadataDialog(items, mediaItemS));
  }
}