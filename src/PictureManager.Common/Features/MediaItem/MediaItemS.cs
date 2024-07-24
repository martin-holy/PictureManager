using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Common.Features.Folder;
using PictureManager.Common.Features.MediaItem.Video;
using PictureManager.Common.Utils;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.MediaItem;

public sealed class MediaItemS(MediaItemR r) : ObservableObject {
  public static Action<MediaItemMetadata, bool> ReadMetadata { get; set; } = null!;

  public void DeleteFromDrive(MediaItemM[] items) =>
    r.ItemsDeleteFromDrive(items);

  public bool Exists(MediaItemM? mi) {
    if (mi == null || File.Exists(mi.FilePath)) return true;
    r.ItemsDelete(new[] { mi });
    return false;
  }

  public void OnMetadataReloaded(RealMediaItemM[] items) {
    r.RaiseMetadataChanged(items.Cast<MediaItemM>().ToArray());
    r.RaiseOrientationChanged(items);
  }

  public Task ReloadMetadata(RealMediaItemM mi) {
    var mim = new MediaItemMetadata(mi);
    if (mi is not VideoM) ReadMetadata(mim, false);

    return Tasks.RunOnUiThread(async () => {
      if (mi is VideoM) ReadMetadata(mim, false);
      if (mim.Success) await mim.FindRefs();
      r.Modify(mi);
      mi.IsOnlyInDb = false;
    });
  }

  public void Rename(RealMediaItemM mi, string newFileName) =>
    r.ItemRename(mi, newFileName);

  public void SetComment(MediaItemM mi, string? comment) {
    mi.Comment = comment;
    mi.SetInfoBox(true);
    mi.OnPropertyChanged(nameof(mi.Comment));
    r.Modify(mi);
  }

  public MediaItemM? GetMediaItem(FolderM folder, string fileName) {
    var mi = folder.MediaItems.SingleOrDefault(x => x.FileName.Equals(fileName, StringComparison.Ordinal));
    return mi != null ? mi : CopyMoveU.CreateMediaItemAndReadMetadata(folder, fileName);
  }
}