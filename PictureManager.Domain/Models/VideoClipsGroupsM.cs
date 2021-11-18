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

    public bool ItemCanRename(string name, MediaItemM mi) =>
      !All.Any(x => x.MediaItem.Equals(mi) && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public VideoClipsGroupM ItemCreate(string name, MediaItemM mi) {
      var group = new VideoClipsGroupM(DataAdapter.GetNextId(), name);
      SetMediaItem(group, mi);
      All.Add(group);

      return group;
    }

    public void ItemRename(VideoClipsGroupM group, string name) {
      group.Name = name;
      DataAdapter.IsModified = true;
    }

    public void ItemDelete(VideoClipsGroupM group) {
      group.MediaItem.VideoClipsGroups.Remove(group);
      All.Remove(group);
      DataAdapter.IsModified = true;
    }

    // TODO
    public void GroupMove(VideoClipsGroupM group, VideoClipsGroupM dest, bool aboveDest) {
      All.Move(group, dest, aboveDest);
      group.MediaItem.VideoClipsGroups.Move(group, dest, aboveDest);
      DataAdapter.IsModified = true;
    }

    public static void SetMediaItem(VideoClipsGroupM group, MediaItemM mi) {
      group.MediaItem = mi;
      mi.VideoClipsGroups ??= new();
      mi.VideoClipsGroups.Add(group);
    }
  }
}
