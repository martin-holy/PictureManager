using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using MH.Utils.Extensions;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class VideoClipsGroupsM {
    private readonly VideoClipsM _videoClips;

    public DataAdapter DataAdapter { get; set; }
    public List<VideoClipsGroupM> All { get; } = new();

    public VideoClipsGroupsM(VideoClipsM vc) {
      _videoClips = vc;
    }

    public bool ItemCanRename(string name, MediaItemM mi) =>
      !All.Any(x => x.MediaItem.Equals(mi) && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public VideoClipsGroupM ItemCreate(string name, MediaItemM mi) {
      var group = new VideoClipsGroupM(DataAdapter.GetNextId(), name);
      group.Items.CollectionChanged += GroupItems_CollectionChanged;
      group.MediaItem = mi;
      group.MediaItem.HasVideoClips = true;
      _videoClips.Items.SetInOrder(group, x => x is VideoClipsGroupM g ? g.Name : string.Empty);
      All.Add(group);
      DataAdapter.IsModified = true;

      return group;
    }

    public void GroupItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
      DataAdapter.IsModified = true;
    }

    public void ItemRename(VideoClipsGroupM group, string name) {
      group.Name = name;
      DataAdapter.IsModified = true;
    }

    public void ItemDelete(VideoClipsGroupM group) {
      // move all group items to root
      foreach (var item in group.Items.Cast<VideoClipM>().ToArray())
        _videoClips.ItemMove(item, _videoClips, false);

      group.Parent.Items.Remove(group);
      group.MediaItem.HasVideoClips = _videoClips.Items.Count != 0;
      All.Remove(group);
      DataAdapter.IsModified = true;
    }

    public void GroupMove(VideoClipsGroupM group, VideoClipsGroupM dest, bool aboveDest) {
      group.Parent.Items.Move(group, dest, aboveDest);
      DataAdapter.IsModified = true;
    }
  }
}
