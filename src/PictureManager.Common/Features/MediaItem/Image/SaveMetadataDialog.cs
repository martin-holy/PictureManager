using MH.UI.Dialogs;
using MH.Utils.Extensions;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.MediaItem.Image;

public sealed class SaveMetadataDialog : ParallelProgressDialog<ImageM> {
  private readonly ImageS _imageS;
  private readonly int _quality;

  public SaveMetadataDialog(ImageM[] items, ImageS imageS, int quality) :
    base("Saving metadata to files...", MH.UI.Res.IconImage, items) {
    _imageS = imageS;
    _quality = quality;
    _autoRun();
  }

  protected override Task _do(ImageM item, CancellationToken token) {
    _reportProgress(item.FilePath);
    _imageS.TryWriteMetadata(item, _quality);

    return Task.CompletedTask;
  }

  public static void Open(ImageM[] items, ImageS imageS, int quality) {
    if (Show(new MessageDialog(
          "Save metadata to files",
          "Do you really want to save image metadata to {0} file{1}?".Plural(items.Length),
          MH.UI.Res.IconQuestion,
          true)) != 1) return;

    Show(new SaveMetadataDialog(items, imageS, quality));
  }
}