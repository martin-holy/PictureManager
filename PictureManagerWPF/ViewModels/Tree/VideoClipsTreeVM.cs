using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using MH.UI.WPF.Interfaces;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.CustomControls;
using PictureManager.Domain;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public sealed class VideoClipsTreeVM : CatTreeViewCategoryBase {
    private readonly VideoClipsM _model;
    private readonly VideoClipsGroupsTreeVM _groupsTreeVM;
    
    public readonly Dictionary<int, VideoClipTreeVM> All = new();
    public event EventHandler<ObjectEventArgs> ItemCreatedEventHandler = delegate { };

    public VideoClipsTreeVM(VideoClipsM model) : base(Category.VideoClips, "Clips") {
      _model = model;
      _groupsTreeVM = new();
      IsExpanded = true;
      CanMoveItem = true;

      _model.Items.CollectionChanged += ModelItems_CollectionChanged;
      _model.VideoClipDeletedEvent += (_, e) => All.Remove(e.VideoClip.Id);

      // load items
      ModelItems_CollectionChanged(_model.Items, null);
    }

    private void ModelItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
      // sync Groups
      _groupsTreeVM.SyncCollection((ObservableCollection<ITreeLeaf>)sender, Items, this, SyncCollection);
      // sync Items
      SyncCollection((ObservableCollection<ITreeLeaf>)sender, Items, this, SyncCollection);
    }

    private void SyncCollection(ObservableCollection<ITreeLeaf> src, ObservableCollection<ITreeLeaf> dest, ITreeBranch parent, MH.Utils.Tree.OnItemsChanged onItemsChanged) {
      MH.Utils.Tree.SyncCollection<VideoClipM, VideoClipTreeVM>(src, dest, parent,
        (model, treeVM) => treeVM.Model.Equals(model),
        model => MH.Utils.Tree.GetDestItem(model, model.Id, All, () => new(model, parent), null));
    }

    public void SetMediaItem(MediaItemM mi) {
      _model.SetCurrentMediaItem(mi);
      UpdateClipsTitles();
      CatTreeView.ExpandAll(this);
    }

    private void UpdateClipsTitles() {
      var nr = 0;
      var clips = Items.OfType<VideoClipsGroupTreeVM>()
        .SelectMany(g => g.Items.Select(x => x))
        .Concat(Items.OfType<VideoClipTreeVM>())
        .Cast<VideoClipTreeVM>();

      foreach (var clip in clips) {
        nr++;
        clip.Title = string.IsNullOrEmpty(clip.Model.Name)
          ? $"Clip #{nr}"
          : clip.Model.Name;
      }
    }

    protected override ICatTreeViewItem ModelItemCreate(ICatTreeViewItem root, string name) {
      var vcm = _model.ItemCreate(ToModel(root) as VideoClipsGroupM, _model.CurrentMediaItem, name);
      var vcvm = All[vcm.Id];
      vcvm.IsSelected = true;
      UpdateClipsTitles();
      ItemCreatedEventHandler(this, new(vcvm));

      return vcvm;
    }

    protected override void ModelItemRename(ICatTreeViewItem item, string name) {
      _model.ItemRename((VideoClipM)ToModel(item), name);
      UpdateClipsTitles();
    }

    protected override void ModelItemDelete(ICatTreeViewItem item) {
      _model.ItemDelete((VideoClipM)ToModel(item));
      UpdateClipsTitles();
    }

    public override void ItemMove(ICatTreeViewItem item, ICatTreeViewItem dest, bool aboveDest) =>
      _model.ItemMove((VideoClipM)ToModel(item), (ITreeLeaf)ToModel(dest), aboveDest);

    protected override string ValidateNewItemName(ICatTreeViewItem root, string name) =>
      null;

    protected override void ModelGroupCreate(ICatTreeViewItem root, string name) =>
      _model.GroupsM.ItemCreate(name, _model.CurrentMediaItem);

    protected override void ModelGroupRename(ICatTreeViewGroup group, string name) =>
      _model.GroupsM.ItemRename((VideoClipsGroupM)ToModel(group), name);

    protected override void ModelGroupDelete(ICatTreeViewGroup group) =>
      _model.GroupsM.ItemDelete((VideoClipsGroupM)ToModel(group));

    public override void GroupMove(ICatTreeViewGroup group, ICatTreeViewGroup dest, bool aboveDest) =>
      _model.GroupsM.GroupMove((VideoClipsGroupM)ToModel(group), (VideoClipsGroupM)ToModel(dest), aboveDest);

    protected override string ValidateNewGroupName(ICatTreeViewItem root, string name) =>
      _model.GroupsM.ItemCanRename(name, _model.CurrentMediaItem)
        ? null
        : $"{name} group already exists!";

    public override string GetTitle(object item) =>
      ToModel(item) switch {
        VideoClipM x => x.Name,
        VideoClipsGroupM x => x.Name,
        _ => null
      };

    private static object ToModel(object item) =>
      item switch {
        VideoClipTreeVM x => x.Model,
        VideoClipsTreeVM x => x._model,
        VideoClipsGroupTreeVM x => x.Model,
        _ => null
      };
  }
}
