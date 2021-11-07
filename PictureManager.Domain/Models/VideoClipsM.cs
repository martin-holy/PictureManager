using System.Collections.Generic;
using System.IO;
using MH.Utils.Extensions;
using PictureManager.Domain.DataAdapters;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class VideoClipsM : ITable {
    private readonly Core _core;

    public DataAdapter DataAdapter { get; }
    public List<IRecord> All { get; } = new();
    public Dictionary<int, VideoClipM> AllDic { get; set; }

    public VideoClipsM(Core core) {
      _core = core;
      DataAdapter = new VideoClipsDataAdapter(core, this);
    }

    public void SetMarker(VideoClipM clip, bool start, int ms, double volume, double speed) {
      clip.SetMarker(start, ms, volume, speed);
      DataAdapter.IsModified = true;
    }

    public VideoClipM ItemCreate(string name, MediaItem mediaItem, VideoClipsGroupM group) {
      var vc = new VideoClipM(DataAdapter.GetNextId(), mediaItem) { Name = name };
      vc.MediaItem.VideoClipAdd(vc, group);
      All.Add(vc);

      return vc;
    }

    public void ItemRename(VideoClipM vc, string name) {
      vc.Name = name;
      DataAdapter.IsModified = true;
    }

    public void ItemDelete(VideoClipM vc) {
      vc.MediaItem.VideoClips?.Remove(vc);
      vc.MediaItem.OnPropertyChanged(nameof(MediaItem.HasVideoClips));
      vc.MediaItem = null;

      if (vc.Group != null) {
        vc.Group.Clips.Remove(vc);
        vc.Group = null;
        _core.VideoClipsGroupsM.DataAdapter.IsModified = true;
      }

      vc.People = null;
      vc.Keywords = null;

      File.Delete(vc.ThumbPath.LocalPath);

      All.Remove(vc);
      DataAdapter.IsModified = true;
    }

    public void ItemMove(VideoClipM vc, VideoClipM dest, bool aboveDest) {
      All.Move(vc, dest, aboveDest);

      if (vc.Group == null)
        vc.MediaItem.VideoClips.Move(vc, dest, aboveDest);
      else
        vc.Group.Clips.Move(vc, dest, aboveDest);

      DataAdapter.IsModified = true;
    }

    public void ItemMove(VideoClipM vc, VideoClipsGroupM dest) {
      if (vc.Group == null)
        vc.MediaItem.VideoClips.Remove(vc);

      vc.Group?.Clips.Remove(vc);
      vc.Group = dest;

      if (dest == null)
        vc.MediaItem.VideoClipAdd(vc, null);
      else
        dest.Clips.Add(vc);

      _core.VideoClipsGroupsM.DataAdapter.IsModified = true;
      DataAdapter.IsModified = true;
    }
  }
}
