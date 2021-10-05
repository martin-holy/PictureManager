using PictureManager.Domain.DataAdapters;
using PictureManager.Domain.Extensions;
using SimpleDB;
using System.Collections.Generic;

namespace PictureManager.Domain.Models {
  public sealed class VideoClipsGroups : ITable {
    public DataAdapter DataAdapter { get; }
    public List<IRecord> All { get; } = new();

    public VideoClipsGroups(Core core) {
      DataAdapter = new VideoClipsGroupsDataAdapter(core, this);
    }

    public VideoClipsGroup ItemCreate(string name, MediaItem mediaItem) {
      var vcg = new VideoClipsGroup(DataAdapter.GetNextId(), name) { MediaItem = mediaItem };
      vcg.MediaItem.VideoClipsGroupAdd(vcg);
      All.Add(vcg);

      return vcg;
    }

    public void ItemRename(VideoClipsGroup vcg, string name) {
      vcg.Name = name;
      DataAdapter.IsModified = true;
    }

    public void ItemDelete(VideoClipsGroup vcg) {
      vcg.MediaItem.VideoClipsGroups.Remove(vcg);
      All.Remove(vcg);
      DataAdapter.IsModified = true;
    }

    public void GroupMove(VideoClipsGroup group, VideoClipsGroup dest, bool aboveDest) {
      All.Move(group, dest, aboveDest);
      group.MediaItem.VideoClipsGroups.Move(group, dest, aboveDest);
      DataAdapter.IsModified = true;
    }
  }
}
