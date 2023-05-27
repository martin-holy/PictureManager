using System;
using System.Collections.Specialized;
using System.Linq;
using MH.Utils;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.DataAdapters;
using PictureManager.Domain.TreeCategories;

namespace PictureManager.Domain.Models {
  public sealed class VideoClipsGroupsM {
    private readonly VideoClipsTreeCategory _treeCategory;
    private readonly MediaItemsM _mediaItemsM;

    public VideoClipsGroupsDataAdapter DataAdapter { get; set; }

    public VideoClipsGroupsM(VideoClipsTreeCategory treeCategory, MediaItemsM mi) {
      _treeCategory = treeCategory;
      _mediaItemsM = mi;
    }

    public bool ItemCanRename(string name, MediaItemM mi) =>
      !DataAdapter.All.Any(x => x.MediaItem.Equals(mi) && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public VideoClipsGroupM ItemCreate(string name, MediaItemM mi) {
      var group = new VideoClipsGroupM(DataAdapter.GetNextId(), name);
      group.Items.CollectionChanged += GroupItems_CollectionChanged;
      group.MediaItem = mi;
      group.MediaItem.HasVideoClips = true;
      Tree.SetInOrder(_treeCategory.Items, group, x => x.Name);
      DataAdapter.All.Add(group);
      _mediaItemsM.MediaItemVideoClips.TryAdd(mi, _treeCategory.Items);

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

      var vcGroup = (VideoClipsGroupM)group;
      vcGroup.MediaItem.HasVideoClips = _treeCategory.Items.Count != 0;
      DataAdapter.All.Remove(vcGroup);
      DataAdapter.IsModified = true;
    }

    public void GroupMove(ITreeGroup group, ITreeGroup dest, bool aboveDest) {
      group.Parent.Items.SetRelativeTo(group, dest, aboveDest);
      DataAdapter.IsModified = true;
    }
  }
}
