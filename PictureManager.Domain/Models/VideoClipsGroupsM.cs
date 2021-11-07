using System;
using System.Collections.Generic;
using System.Linq;
using MH.Utils.Extensions;
using PictureManager.Domain.DataAdapters;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class VideoClipsGroupsM {
    public DataAdapter DataAdapter { get; }
    public List<VideoClipsGroupM> All { get; } = new();

    public VideoClipsGroupsM(Core core) {
      DataAdapter = new VideoClipsGroupsDataAdapter(core, this);
    }

    public bool ItemCanRename(string name, MediaItem mediaItem) =>
      !All.Any(x => x.MediaItem.Equals(mediaItem) && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public VideoClipsGroupM ItemCreate(string name, MediaItem mediaItem) {
      var vcg = new VideoClipsGroupM(DataAdapter.GetNextId(), name) { MediaItem = mediaItem };
      vcg.MediaItem.VideoClipsGroupAdd(vcg);
      All.Add(vcg);

      return vcg;
    }

    public void ItemRename(VideoClipsGroupM vcg, string name) {
      vcg.Name = name;
      DataAdapter.IsModified = true;
    }

    public void ItemDelete(VideoClipsGroupM vcg) {
      vcg.MediaItem.VideoClipsGroups.Remove(vcg);
      All.Remove(vcg);
      DataAdapter.IsModified = true;
    }

    public void GroupMove(VideoClipsGroupM group, VideoClipsGroupM dest, bool aboveDest) {
      All.Move(group, dest, aboveDest);
      group.MediaItem.VideoClipsGroups.Move(group, dest, aboveDest);
      DataAdapter.IsModified = true;
    }
  }
}
