using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using MH.Utils;
using MH.Utils.Interfaces;
using PictureManager.Domain.EventsArgs;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class VideoClipsM : ITreeBranch {
    #region ITreeBranch implementation
    public ITreeBranch Parent { get; set; }
    public ObservableCollection<ITreeLeaf> Items { get; set; } = new();
    #endregion

    public DataAdapter DataAdapter { get; set; }
    public List<VideoClipM> All { get; } = new();
    public Dictionary<int, VideoClipM> AllDic { get; set; }
    public MediaItemM CurrentMediaItem { get; set; }
    public VideoClipM CurrentVideoClip { get; set; }
    public VideoClipsGroupsM GroupsM { get; }

    public event EventHandler<VideoClipDeletedEventArgs> VideoClipDeletedEvent = delegate { };

    public VideoClipsM() {
      GroupsM = new(this);
    }

    private static string GetItemName(object item) =>
      item is VideoClipM vc
        ? vc.Name
        : string.Empty;

    public void SetMarker(VideoClipM clip, bool start, int ms, double volume, double speed) {
      clip.SetMarker(start, ms, volume, speed);
      DataAdapter.IsModified = true;
    }

    public VideoClipM ItemCreate(ITreeBranch group, MediaItemM mi, string name) {
      var root = group ?? this;
      var item = new VideoClipM(DataAdapter.GetNextId(), mi) {
        Parent = root,
        Name = name
      };

      root.Items.Add(item);
      CurrentVideoClip = item;
      mi.HasVideoClips = true;
      All.Add(item);
      DataAdapter.IsModified = true;

      return item;
    }

    public void ItemRename(VideoClipM vc, string name) {
      vc.Name = name;
      DataAdapter.IsModified = true;
    }

    public void ItemDelete(VideoClipM vc) {
      File.Delete(vc.ThumbPath);

      vc.MediaItem.HasVideoClips = Items.Count != 0;
      vc.MediaItem = null;
      vc.Parent.Items.Remove(vc);
      vc.Parent = null;
      vc.People = null;
      vc.Keywords = null;

      All.Remove(vc);
      VideoClipDeletedEvent(this, new(vc));
      DataAdapter.IsModified = true;
    }

    public void ItemMove(VideoClipM item, ITreeLeaf dest, bool aboveDest) {
      Tree.ItemMove(item, dest, aboveDest, GetItemName);
      DataAdapter.IsModified = true;
    }

    public void SetCurrentMediaItem(MediaItemM mi) {
      CurrentMediaItem = mi;
      Items.Clear();
      if (mi == null) return;

      foreach (var group in GroupsM.All.Where(x => x.MediaItem.Equals(mi)))
        Items.Add(group);

      foreach (var clip in All.Where(x => Equals(x.Parent, this) && x.MediaItem.Equals(mi)))
        Items.Add(clip);
    }

    public VideoClipM GetNextClip(bool inGroup, bool selectFirst) {
      var groups = new List<List<VideoClipM>>();

      groups.AddRange(Items.OfType<VideoClipsGroupM>()
        .Where(g => g.Items.Count > 0)
        .Select(g => g.Items.Cast<VideoClipM>().ToList()));

      var clips = Items.OfType<VideoClipM>().ToList();
      if (clips.Count != 0) 
        groups.Add(clips);

      if (groups.Count == 0)
        return null;

      if (selectFirst)
        return groups[0][0];

      for (var i = 0; i < groups.Count; i++) {
        var group = groups[i];
        var idx = group.IndexOf(CurrentVideoClip);

        if (idx < 0) continue;

        if (idx < group.Count - 1)
          return group[idx + 1];

        return inGroup
          ? group[0]
          : groups[i < groups.Count - 1 ? i + 1 : 0][0];
      }

      return null;
    }
  }
}
