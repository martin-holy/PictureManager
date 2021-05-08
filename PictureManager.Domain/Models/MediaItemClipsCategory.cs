﻿using System;
using System.Linq;
using PictureManager.Domain.CatTreeViewModels;

namespace PictureManager.Domain.Models {
  public sealed class MediaItemClipsCategory: CatTreeViewBaseItem, ICatTreeViewCategory {
    public MediaItem CurrentMediaItem { get; private set; }
    public Category Category { get; }
    public bool CanHaveGroups { get; set; }
    public bool CanHaveSubItems { get; set; }
    public bool CanModifyItems { get; set; }
    public IconName CategoryGroupIconName { get; }

    public MediaItemClipsCategory() {
      Title = "Clips";
      IconName = IconName.MovieClapper;
      Category = Category.MediaItemClips;
      CategoryGroupIconName = CatTreeViewUtils.GetCategoryGroupIconName(Category);
      CanHaveGroups = true;
      CanModifyItems = true;
      IsExpanded = true;
    }

    public void SetMediaItem(MediaItem mi) {
      // clear previous items
      foreach (var group in Items.OfType<ICatTreeViewGroup>())
        group.Items.Clear();
      Items.Clear();

      CurrentMediaItem = mi;
      if (mi == null) return;

      // add groups
      if (mi.VideoClipsGroups != null)
        foreach (var group in mi.VideoClipsGroups) {
          var groupItem = CreateGroupItem(group, this);

          // add clips in groups
          foreach (var clip in group.Clips)
            groupItem.Items.Add(CreateClipItem(clip, groupItem));

          Items.Add(groupItem);
        }

      // add clips without groups
      foreach (var clip in mi.VideoClips.Where(vc => vc.Group == null))
        Items.Add(CreateClipItem(clip, this));
    }

    private static ICatTreeViewGroup CreateGroupItem(VideoClipsGroup group, ICatTreeViewBaseItem parent) {
      return new CatTreeViewGroup {
        Parent = parent,
        Title = group.Name,
        IconName = IconName.Folder,
        Tag = group
      };
    }

    private static ICatTreeViewBaseItem CreateClipItem(VideoClip clip, ICatTreeViewBaseItem parent) {
      return new CatTreeViewBaseItem {
        Parent = parent,
        Title = string.IsNullOrEmpty(clip.Name) ? $"Clip #{parent.Items.Count(x => !(x is ICatTreeViewGroup)) + 1}" : clip.Name,
        IconName = IconName.MovieClapper,
        Tag = clip
      };
    }

    public string ValidateNewItemTitle(ICatTreeViewBaseItem root, string name) {
      return root.Items.SingleOrDefault(x => !(x is ICatTreeViewGroup) && x.Title.Equals(name)) != null
        ? $"{name} item already exists!"
        : null;
    }

    public void ItemCreate(ICatTreeViewBaseItem root, string name) {
      var vc = Core.Instance.VideoClips.ItemCreate(CurrentMediaItem, root.Tag as VideoClipsGroup);
      root.Items.Add(CreateClipItem(vc, root));
    }

    public void ItemRename(ICatTreeViewBaseItem item, string name) {
      throw new NotImplementedException();
    }

    public void ItemDelete(ICatTreeViewBaseItem item) {
      Core.Instance.VideoClips.ItemDelete(item.Tag as VideoClip);
      item.Parent.Items.Remove(item);
    }

    public string ValidateNewGroupTitle(ICatTreeViewBaseItem root, string name) {
      return root.Items.OfType<ICatTreeViewGroup>().SingleOrDefault(x => x.Title.Equals(name)) != null
        ? $"{name} group already exists!"
        : null;
    }

    public void GroupCreate(ICatTreeViewBaseItem root, string name) {
      var vcg = Core.Instance.VideoClipsGroups.ItemCreate(name, CurrentMediaItem);
      root.Items.Insert(root.Items.Count(x => x is ICatTreeViewGroup), CreateGroupItem(vcg, root));
    }

    public void GroupRename(ICatTreeViewGroup group, string newTitle) {
      group.Title = newTitle;
      Core.Instance.VideoClipsGroups.ItemRename(group.Tag as VideoClipsGroup, newTitle);
    }

    public void GroupDelete(ICatTreeViewGroup group) {
      Core.Instance.VideoClipsGroups.ItemDelete(group.Tag as VideoClipsGroup);
      group.Parent.Items.Remove(group);
    }

    public void GroupMove(ICatTreeViewGroup group, ICatTreeViewGroup dest, bool aboveDest) {
      Core.Instance.VideoClipsGroups.GroupMove(group.Tag as VideoClipsGroup, dest.Tag as VideoClipsGroup, aboveDest);
      group.Parent.Items.Move(group, dest, aboveDest);
    }
  }
}
