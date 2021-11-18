using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using MH.UI.WPF.Interfaces;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.CustomControls;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Domain.Utils;
using PictureManager.Properties;

namespace PictureManager.ViewModels.Tree {
  public sealed class VideoClipsTreeVM : CatTreeViewCategoryBase {
    private readonly Core _core;

    public VideoClipsM Model { get; }
    public MediaItemM CurrentMediaItem { get; private set; }

    public VideoClipsTreeVM(Core core, VideoClipsM model) : base(Category.VideoClips, "Clips") {
      _core = core;
      Model = model;
      IsExpanded = true;
    }

    public void SetMediaItem(MediaItemM mi) {
      // clear previous items
      foreach (var group in Items.OfType<ICatTreeViewGroup>())
        group.Items.Clear();
      Items.Clear();

      CurrentMediaItem = mi;
      if (mi == null) return;
      
      // add groups
      if (mi.VideoClipsGroups != null)
        foreach (var group in mi.VideoClipsGroups) {
          var groupItem = new VideoClipsGroupTreeVM(group, this);

          // add clips in groups
          foreach (var clip in group.Clips)
            groupItem.Items.Add(CreateClipItem(clip, groupItem));

          Items.Add(groupItem);
        }

      // add clips without groups
      if (mi.VideoClips != null)
        foreach (var clip in mi.VideoClips)
          Items.Add(CreateClipItem(clip, this));
      
      CatTreeView.ExpandAll(this);
    }

    public static void CreateThumbnail(VideoClipM vc, FrameworkElement visual, bool reCreate = false) {
      if (!File.Exists(vc.ThumbPath) || reCreate) {
        Imaging.CreateVideoThumbnailFromVisual(visual, vc.ThumbPath, Settings.Default.ThumbnailSize, Settings.Default.JpegQualityLevel);

        vc.OnPropertyChanged(nameof(vc.ThumbPath));
      }
    }

    public void SelectNext(VideoClipTreeVM current, bool inGroup) {
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

    private static VideoClipTreeVM CreateClipItem(VideoClipM clip, ITreeBranch parent) =>
      new(clip) {
        Parent = parent,
        Title = string.IsNullOrEmpty(clip.Name) ? $"Clip #{parent.Items.Count(x => x is not ICatTreeViewGroup) + 1}" : clip.Name
      };

    // update clip titles without names
    private static void UpdateClipTitles(IEnumerable<VideoClipTreeVM> items) {
      foreach (var vcvm in items)
        if (string.IsNullOrEmpty(vcvm.Model.Name)) {
          var pi = vcvm.Parent.Items;
          vcvm.Title = $"Clip #{pi.IndexOf(vcvm) - pi.Count(x => x is ICatTreeViewGroup) + 1}";
        }
    }

    /*public override void ItemMove(ICatTreeViewItem item, ICatTreeViewItem dest, bool aboveDest) {
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
    }*/

    /*public override void GroupMove(ICatTreeViewGroup group, ICatTreeViewGroup dest, bool aboveDest) {
      App.Core.VideoClipsGroups.GroupMove(group.Tag as VideoClipsGroup, dest.Tag as VideoClipsGroup, aboveDest);
      group.Parent.Items.Move(group, dest, aboveDest);
    }*/

    protected override ICatTreeViewItem ModelItemCreate(ICatTreeViewItem root, string name) {
      var vcm = Model.ItemCreate(name, CurrentMediaItem, ToModel(root) as VideoClipsGroupM);
      var vcvm = CreateClipItem(vcm, root);
      vcvm.IsSelected = true;
      root.Items.Add(vcvm);

      var vp = App.WMain.MediaViewer.FullVideo;
      Model.SetMarker(vcm, true, (int)Math.Round(vp.TimelineSlider.Value), vp.VolumeSlider.Value, vp.SpeedSlider.Value);
      CreateThumbnail(vcm, vp.Player, true);

      App.WMain.ToolsTabs.VideoClips.CtvClips.ScrollTo(vcvm);

      return vcvm;
    }

    protected override void ModelItemRename(ICatTreeViewItem item, string name) =>
      Model.ItemRename((VideoClipM)ToModel(item), name);

    protected override void ModelItemDelete(ICatTreeViewItem item) {
      item.Parent.Items.Remove(item);
      UpdateClipTitles(item.Parent.Items.OfType<VideoClipTreeVM>());
      item.Parent = null;

      Model.ItemDelete((VideoClipM)ToModel(item));
    }

    protected override string ValidateNewItemName(ICatTreeViewItem root, string name) => null;

    protected override void ModelGroupCreate(ICatTreeViewItem root, string name) {
      var vgm = _core.VideoClipsGroupsM.ItemCreate(name, CurrentMediaItem);
      var vgvm = new VideoClipsGroupTreeVM(vgm, root);
      root.Items.SetInOrder(vgvm, x => x is VideoClipsGroupTreeVM g ? g.Model.Name : string.Empty);
    }

    protected override void ModelGroupRename(ICatTreeViewGroup group, string name) =>
      _core.VideoClipsGroupsM.ItemRename((VideoClipsGroupM)ToModel(group), name);

    protected override void ModelGroupDelete(ICatTreeViewGroup group) {
      foreach (var item in group.Items.Cast<ICatTreeViewItem>())
        ItemDelete(item);

      group.Parent.Items.Remove(group);
      group.Parent = null;
      _core.VideoClipsGroupsM.ItemDelete((VideoClipsGroupM)ToModel(group));
    }

    protected override string ValidateNewGroupName(ICatTreeViewItem root, string name) =>
      _core.VideoClipsGroupsM.ItemCanRename(name, CurrentMediaItem) ? null : $"{name} group already exists!";

    public override string GetTitle(object item) =>
      ToModel(item) switch {
        VideoClipM x => x.Name,
        VideoClipsGroupM x => x.Name,
        _ => null
      };

    private static object ToModel(object item) =>
      item switch {
        VideoClipTreeVM x => x.Model,
        VideoClipsTreeVM x => x.Model,
        VideoClipsGroupTreeVM x => x.Model,
        _ => null
      };
  }
}
