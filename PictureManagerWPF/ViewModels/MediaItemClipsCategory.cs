using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PictureManager.Domain;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
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
      if (mi.VideoClips != null)
        foreach (var clip in mi.VideoClips)
          Items.Add(CreateClipItem(clip, this));

      ExpandAll(this);
    }

    public void SelectNext(VideoClipViewModel current, bool inGroup) {
      var groups = new List<List<ICatTreeViewBaseItem>>();
      groups.AddRange(Items.Where(x => x is ICatTreeViewGroup g && g.Items.Count > 0).Select(g => g.Items.ToList()));
      groups.Add(Items.Where(x => !(x is ICatTreeViewGroup)).ToList());

      for (var i = 0; i < groups.Count; i++) {
        var group = groups[i];
        var idx = group.IndexOf(current);

        if (idx < 0) continue;

        ICatTreeViewBaseItem next;

        if (idx < group.Count - 1)
          next = group[idx + 1];
        else
          next = inGroup ? group[0] : groups[i < groups.Count - 1 ? i + 1 : 0][0];

        if (next == current)
          next.IsSelected = false;

        next.IsSelected = true;

        break;
      }
    }

    public static string GetDuration(int start, int end) {
      if (end == 0) return string.Empty;

      string format;
      var ms = end - start;
      if (ms >= 60 * 60 * 1000) format = @"h\:mm\:ss\.f";
      else if (ms >= 60 * 1000) format = @"m\:ss\.f";
      else format = @"s\.f\s";

      return TimeSpan.FromMilliseconds(ms).ToString(format);
    }

    public static string FormatTimeSpan(int ms) {
      if (ms == 0) return string.Empty;

      var format = ms >= 60 * 60 * 1000 ? @"h\:mm\:ss\.fff" : @"m\:ss\.fff";

      return TimeSpan.FromMilliseconds(ms).ToString(format);
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
      return new VideoClipViewModel(clip) {
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
      var vc = Core.Instance.VideoClips.ItemCreate(name, CurrentMediaItem, root.Tag as VideoClipsGroup);
      if (!(CreateClipItem(vc, root) is VideoClipViewModel vcvm)) return;

      var player = App.WMain.FullMedia.Player;
      vcvm.SetMarker(true, player.Position, player.Volume, player.SpeedRatio);
      vcvm.IsSelected = true;
      root.Items.Add(vcvm);

      CurrentMediaItem.OnPropertyChanged(nameof(MediaItem.HasVideoClips));

      App.WMain.FullMedia.CreateThumbnail(vcvm, player, true);
    }

    public void ItemRename(ICatTreeViewBaseItem item, string name) {
      item.Title = name;
      Core.Instance.VideoClips.ItemRename(item.Tag as VideoClip, name);
    }

    public void ItemDelete(ICatTreeViewBaseItem item) {
      if (!(item is VideoClipViewModel vc)) return;

      File.Delete(vc.ThumbPath.LocalPath);
      item.Parent.Items.Remove(item);
      Core.Instance.VideoClips.ItemDelete(item.Tag as VideoClip);

      CurrentMediaItem.OnPropertyChanged(nameof(MediaItem.HasVideoClips));
    }

    public void ItemMove(ICatTreeViewBaseItem item, ICatTreeViewBaseItem dest, bool aboveDest) {
      // move item to end of category or group
      if (dest is ICatTreeViewCategory || dest is ICatTreeViewGroup) {
        Core.Instance.VideoClips.ItemMove(item.Tag as VideoClip, dest.Tag as VideoClipsGroup);
        item.Parent.Items.Remove(item);
        dest.Items.Add(item);
        item.Parent = dest;
      }
      else {
        // update parent 
        if (item.Parent != dest.Parent) {
          Core.Instance.VideoClips.ItemMove(item.Tag as VideoClip, dest.Parent.Tag as VideoClipsGroup);
          item.Parent.Items.Remove(item);
          dest.Parent.Items.Add(item);
          item.Parent = dest.Parent;
        }
        Core.Instance.VideoClips.ItemMove(item.Tag as VideoClip, dest.Tag as VideoClip, aboveDest);
        item.Parent.Items.Move(item, dest, aboveDest);
      }
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

    public void GroupRename(ICatTreeViewGroup group, string name) {
      group.Title = name;
      Core.Instance.VideoClipsGroups.ItemRename(group.Tag as VideoClipsGroup, name);
    }

    public void GroupDelete(ICatTreeViewGroup group) {
      foreach (var item in group.Items)
        ItemDelete(item);

      group.Parent.Items.Remove(group);
      group.Parent = null;
      Core.Instance.VideoClipsGroups.ItemDelete(group.Tag as VideoClipsGroup);
    }

    public void GroupMove(ICatTreeViewGroup group, ICatTreeViewGroup dest, bool aboveDest) {
      Core.Instance.VideoClipsGroups.GroupMove(group.Tag as VideoClipsGroup, dest.Tag as VideoClipsGroup, aboveDest);
      group.Parent.Items.Move(group, dest, aboveDest);
    }
  }
}
