using MH.UI.Dialogs;
using MH.Utils.Extensions;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.MediaItem.Image;

public sealed class SaveMetadataDialog : ParallelProgressDialog<ImageM> {
  private readonly ImageS _imageS;

  public SaveMetadataDialog(ImageM[] items, ImageS imageS) :
    base("Saving metadata to files...", MH.UI.Res.IconImage, items, null, null) {
    _imageS = imageS;
    AutoRun();
  }

  protected override Task Do(ImageM item) {
    ReportProgress(item.FilePath);
    _imageS.TryWriteMetadata(item);

    return Task.CompletedTask;
  }

  public static void Open(ImageM[] items, ImageS imageS) {
    if (Show(new MessageDialog(
          "Save metadata to files",
          "Do you really want to save image metadata to {0} file{1}?".Plural(items.Length),
          MH.UI.Res.IconQuestion,
          true)) != 1) return;

    Show(new SaveMetadataDialog(items, imageS));
  }
}