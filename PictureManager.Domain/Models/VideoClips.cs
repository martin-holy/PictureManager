using PictureManager.Domain.DataAdapters;
using PictureManager.Domain.Extensions;
using SimpleDB;
using System.Collections.Generic;

namespace PictureManager.Domain.Models {
  public sealed class VideoClips : ITable {
    private readonly Core _core;

    public DataAdapter DataAdapter { get; }
    public List<IRecord> All { get; } = new();
    public Dictionary<int, VideoClip> AllDic { get; set; }

    public VideoClips(Core core) {
      _core = core;
      DataAdapter = new VideoClipsDataAdapter(core, this);
    }

    public VideoClip ItemCreate(string name, MediaItem mediaItem, VideoClipsGroup group) {
      var vc = new VideoClip(DataAdapter.GetNextId(), mediaItem) { Name = name };
      vc.MediaItem.VideoClipAdd(vc, group);
      All.Add(vc);

      return vc;
    }

    public void ItemRename(VideoClip vc, string name) {
      vc.Name = name;
      DataAdapter.IsModified = true;
    }

    public void ItemDelete(VideoClip vc) {
      vc.MediaItem.VideoClips?.Remove(vc);
      vc.MediaItem = null;

      if (vc.Group != null) {
        vc.Group.Clips.Remove(vc);
        vc.Group = null;
        _core.VideoClipsGroups.DataAdapter.IsModified = true;
      }

      vc.People = null;
      vc.Keywords = null;

      All.Remove(vc);
      DataAdapter.IsModified = true;
    }

    public void ItemMove(VideoClip vc, VideoClip dest, bool aboveDest) {
      All.Move(vc, dest, aboveDest);

      if (vc.Group == null)
        vc.MediaItem.VideoClips.Move(vc, dest, aboveDest);
      else
        vc.Group.Clips.Move(vc, dest, aboveDest);

      DataAdapter.IsModified = true;
    }

    public void ItemMove(VideoClip vc, VideoClipsGroup dest) {
      if (vc.Group == null)
        vc.MediaItem.VideoClips.Remove(vc);

      vc.Group?.Clips.Remove(vc);
      vc.Group = dest;

      if (dest == null)
        vc.MediaItem.VideoClipAdd(vc, null);
      else
        dest.Clips.Add(vc);

      _core.VideoClipsGroups.DataAdapter.IsModified = true;
      DataAdapter.IsModified = true;
    }
  }
}
