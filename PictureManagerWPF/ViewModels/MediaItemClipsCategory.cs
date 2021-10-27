using PictureManager.Domain;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Extensions;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PictureManager.ViewModels {
  public sealed class MediaItemClipsCategory : CatTreeViewCategory {
    public MediaItem CurrentMediaItem { get; private set; }

    public MediaItemClipsCategory() : base(Category.MediaItemClips) {
      Title = "Clips";
      CanHaveGroups = true;
      CanCreateItems = true;
      CanRenameItems = true;
      CanDeleteItems = true;
      CanMoveItem = true;
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

      CatTreeViewUtils.ExpandAll(this);
    }

    public void SelectNext(VideoClipViewModel current, bool inGroup) {
      var groups = new List<List<ICatTreeViewItem>>();
      groups.AddRange(Items.Where(x => x is ICatTreeViewGroup g && g.Items.Count > 0).Select(g => ((ICatTreeViewItem)g).Items.Cast<ICatTreeViewItem>().ToList()));

      if (Items.Any(x => x is not ICatTreeViewGroup))
        groups.Add(Items.Where(x => x is not ICatTreeViewGroup).Cast<ICatTreeViewItem>().ToList());

      if (groups.Count == 0) return;

      // select first
      if (current == null) {
        groups[0][0].IsSelected = true;
        return;
      }

      for (var i = 0; i < groups.Count; i++) {
        var group = groups[i];
        var idx = group.IndexOf(current);

        if (idx < 0) continue;

        ICatTreeViewItem next;

        if (idx < group.Count - 1)
          next = group[idx + 1];
        else
          next = inGroup ? group[0] : groups[i < groups.Count - 1 ? i + 1 : 0][0];

        if (Equals(next, current))
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
      var format = ms >= 60 * 60 * 1000 ? @"h\:mm\:ss\.fff" : @"m\:ss\.fff";

      return TimeSpan.FromMilliseconds(ms).ToString(format);
    }

    private static ICatTreeViewGroup CreateGroupItem(VideoClipsGroup group, ICatTreeViewItem parent) =>
      new CatTreeViewGroup {
        Parent = parent,
        Title = group.Name,
        IconName = IconName.Folder,
        Tag = group
      };

    private static ICatTreeViewItem CreateClipItem(VideoClip clip, ICatTreeViewItem parent) =>
      new VideoClipViewModel(clip) {
        Parent = parent,
        Title = string.IsNullOrEmpty(clip.Name) ? $"Clip #{parent.Items.Count(x => x is not ICatTreeViewGroup) + 1}" : clip.Name,
        IconName = IconName.MovieClapper,
        Tag = clip
      };

    // update clip titles without names
    private static void UpdateClipTitles(IEnumerable<VideoClipViewModel> items) {
      foreach (var baseItem in items)
        if (string.IsNullOrEmpty((baseItem.Tag as VideoClip)?.Name)) {
          var pi = baseItem.Parent.Items;
          baseItem.Title = $"Clip #{pi.IndexOf(baseItem) - pi.Count(x => x is ICatTreeViewGroup) + 1}";
        }
    }

    public override ICatTreeViewItem ItemCreate(ICatTreeViewItem root, string name) {
      var vc = App.Core.VideoClips.ItemCreate(name, CurrentMediaItem, root.Tag as VideoClipsGroup);
      if (CreateClipItem(vc, root) is not VideoClipViewModel vcvm) return null;

      var player = App.WMain.MediaViewer.FullVideo.Player;
      App.WMain.MediaViewer.FullVideo.SetMarker(vcvm, true);
      vcvm.IsSelected = true;
      root.Items.Add(vcvm);
      App.WMain.ToolsTabs.VideoClips.CtvClips.ScrollTo(vcvm);

      CurrentMediaItem.OnPropertyChanged(nameof(MediaItem.HasVideoClips));

      CustomControls.VideoPlayer.CreateThumbnail(vcvm, player, true);

      return vcvm;
    }

    public override void ItemRename(ICatTreeViewItem item, string name) {
      item.Title = name;
      App.Core.VideoClips.ItemRename(item.Tag as VideoClip, name);
    }

    public override void ItemDelete(ICatTreeViewItem item) {
      if (item is not VideoClipViewModel vc) return;

      File.Delete(vc.ThumbPath.LocalPath);
      item.Parent.Items.Remove(item);
      UpdateClipTitles(item.Parent.Items.OfType<VideoClipViewModel>());
      item.Parent = null;
      App.Core.VideoClips.ItemDelete(item.Tag as VideoClip);

      CurrentMediaItem.OnPropertyChanged(nameof(MediaItem.HasVideoClips));
    }

    public override void ItemMove(ICatTreeViewItem item, ICatTreeViewItem dest, bool aboveDest) {
      var items = item.Parent.Items.Union(dest is VideoClipViewModel ? dest.Parent.Items : dest.Items).OfType<VideoClipViewModel>();

      // move item to end of category or group
      if (dest is ICatTreeViewCategory or ICatTreeViewGroup) {
        App.Core.VideoClips.ItemMove(item.Tag as VideoClip, dest.Tag as VideoClipsGroup);
        item.Parent.Items.Remove(item);
        dest.Items.Add(item);
        item.Parent = dest;
      }
      else {
        // update parent 
        if (!Equals(item.Parent, dest.Parent)) {
          App.Core.VideoClips.ItemMove(item.Tag as VideoClip, ((ICatTreeViewItem)dest.Parent).Tag as VideoClipsGroup);
          item.Parent.Items.Remove(item);
          dest.Parent.Items.Add(item);
          item.Parent = dest.Parent;
        }
        App.Core.VideoClips.ItemMove(item.Tag as VideoClip, dest.Tag as VideoClip, aboveDest);
        item.Parent.Items.Move(item, dest, aboveDest);
      }

      UpdateClipTitles(items);
    }

    public override void GroupCreate(ICatTreeViewCategory root, string name) {
      var vcg = App.Core.VideoClipsGroups.ItemCreate(name, CurrentMediaItem);
      var ctvg = CreateGroupItem(vcg, root);
      root.Items.Insert(root.Items.Count(x => x is ICatTreeViewGroup), ctvg);
    }

    public override void GroupRename(ICatTreeViewGroup group, string name) {
      group.Title = name;
      App.Core.VideoClipsGroups.ItemRename(group.Tag as VideoClipsGroup, name);
    }

    public override void GroupDelete(ICatTreeViewGroup group) {
      foreach (var item in group.Items.Cast<ICatTreeViewItem>())
        ItemDelete(item);

      group.Parent.Items.Remove(group);
      group.Parent = null;
      App.Core.VideoClipsGroups.ItemDelete(group.Tag as VideoClipsGroup);
    }

    public override void GroupMove(ICatTreeViewGroup group, ICatTreeViewGroup dest, bool aboveDest) {
      App.Core.VideoClipsGroups.GroupMove(group.Tag as VideoClipsGroup, dest.Tag as VideoClipsGroup, aboveDest);
      group.Parent.Items.Move(group, dest, aboveDest);
    }
  }
}
