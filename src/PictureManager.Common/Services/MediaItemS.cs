﻿using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Common.HelperClasses;
using PictureManager.Common.Models.MediaItems;
using PictureManager.Common.Repositories;
using PictureManager.Interfaces.Models;
using PictureManager.Interfaces.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Common.Services;

public sealed class MediaItemS(MediaItemR r) : ObservableObject, IMediaItemS {
  public static Action<MediaItemMetadata, bool> ReadMetadata { get; set; }

  public void DeleteFromDrive(MediaItemM[] items) =>
    r.ItemsDeleteFromDrive(items);

  public bool Exists(MediaItemM mi) {
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

  public void SetComment(MediaItemM mi, string comment) {
    mi.Comment = comment;
    mi.SetInfoBox(true);
    mi.OnPropertyChanged(nameof(mi.Comment));
    r.Modify(mi);
  }

  public IMediaItemM ImportMediaItem(string folderPath, string fileName) {
    return null; // TODO
  }
}