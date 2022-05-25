using System;
using System.Collections.Specialized;
using System.Linq;
using MH.Utils;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.DataAdapters;

namespace PictureManager.Domain.Models {
  public sealed class VideoClipsGroupsM {
    private readonly VideoClipsM _videoClips;
    private readonly MediaItemsM _mediaItemsM;

    public VideoClipsGroupsDataAdapter DataAdapter { get; set; }

    public VideoClipsGroupsM(VideoClipsM vc, MediaItemsM mi) {
      _videoClips = vc;
      _mediaItemsM = mi;
    }

    public bool ItemCanRename(string name, MediaItemM mi) =>
      !DataAdapter.All.Values.Any(x => x.MediaItem.Equals(mi) && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public VideoClipsGroupM ItemCreate(string name, MediaItemM mi) {
      var group = new VideoClipsGroupM(DataAdapter.GetNextId(), name);
      group.Items.CollectionChanged += GroupItems_CollectionChanged;
      group.MediaItem = mi;
      group.MediaItem.HasVideoClips = true;
      Tree.SetInOrder(_videoClips.Items, group, x => x.Name);
      DataAdapter.All.Add(group.Id, group);

      if (!_mediaItemsM.MediaItemVideoClips.ContainsKey(mi))
        _mediaItemsM.MediaItemVideoClips.Add(mi, _videoClips.Items);

      return group;
    }

    public void GroupItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
      DataAdapter.IsModified = true;
    }

    public void ItemRename(ITreeGroup group, string name) {
      group.Name = name;
      DataAdapter.IsModified = true;
    }

    public void ItemDelete(ITreeItem group) {
      // move all group items to root
      if (Tree.GetTopParent(group) is ITreeCategory cat)
        foreach (var item in group.Items.ToArray())
          cat.ItemMove(item, cat, false);

      group.Parent.Items.Remove(group);
      ((VideoClipsGroupM)group).MediaItem.HasVideoClips = _videoClips.Items.Count != 0;
      DataAdapter.All.Remove(((VideoClipsGroupM)group).Id);
      DataAdapter.IsModified = true;
    }

    public void GroupMove(ITreeGroup group, ITreeGroup dest, bool aboveDest) {
      group.Parent.Items.SetRelativeTo(group, dest, aboveDest);
      DataAdapter.IsModified = true;
    }
  }
}
