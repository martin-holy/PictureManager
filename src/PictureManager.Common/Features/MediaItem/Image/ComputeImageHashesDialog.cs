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
    base("Computing Hashes ...", Res.IconCompare, items) {
    _hashes = hashes;
    _hashMethod = hashMethod;
    _autoRun();
  }

  protected override Task _do(MediaItemM item, CancellationToken token) {
    _reportProgress(item.FilePath);

    if (_hashes.ContainsKey(item)) return Task.CompletedTask;

    if (!File.Exists(item.FilePathCache))
      MediaItemS.CreateImageThumbnail(item);

    _hashes.Add(item, _hashMethod(item.FilePathCache));

    return Task.CompletedTask;
  }
}