using MH.UI.Dialogs;
using MH.Utils;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.MediaItem.Image;

public sealed class ComputeImageHashesDialog : ProgressDialog<MediaItemM> {
  private readonly Dictionary<object, long> _hashes;
  private readonly Imaging.ImageHashFunc _hashMethod;

  public ComputeImageHashesDialog(MediaItemM[] items, Dictionary<object, long> hashes, Imaging.ImageHashFunc hashMethod) :
    base("Computing Hashes ...", Res.IconCompare, items, null, null) {
    _hashes = hashes;
    _hashMethod = hashMethod;
    AutoRun();
  }

  protected override Task Do(MediaItemM item, CancellationToken token) {
    ReportProgress(item.FilePath);

    if (_hashes.ContainsKey(item)) return Task.CompletedTask;

    if (!File.Exists(item.FilePathCache))
      MediaItemS.CreateImageThumbnail(item);

    _hashes.Add(item, _hashMethod(item.FilePathCache));

    return Task.CompletedTask;
  }
}