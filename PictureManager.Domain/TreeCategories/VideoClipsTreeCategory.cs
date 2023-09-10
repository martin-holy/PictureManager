using MH.Utils;
using MH.Utils.Interfaces;
using PictureManager.Domain.BaseClasses;
using PictureManager.Domain.Models;

namespace PictureManager.Domain.TreeCategories {
  public sealed class VideoClipsTreeCategory : TreeCategoryBase {
    private readonly VideoClipsM _videoClipsM;
    public VideoClipsGroupsM GroupsM { get; }

    public VideoClipsTreeCategory(VideoClipsM videoClipsM, MediaItemsM mediaItemsM) :
      base(Res.IconMovieClapper, Category.VideoClips, "Clips") {
      _videoClipsM = videoClipsM;
      GroupsM = new(this, mediaItemsM);
      IsExpanded = true;
      CanMoveItem = true;
    }

    protected override ITreeItem ModelItemCreate(ITreeItem root, string name) =>
      _videoClipsM.Create(root, name);

    protected override void ModelItemRename(ITreeItem item, string name) =>
      _videoClipsM.Rename(item, name);

    protected override void ModelItemDelete(ITreeItem item) =>
      _videoClipsM.Delete(item);

    public override void ItemMove(ITreeItem item, ITreeItem dest, bool aboveDest) {
      Tree.ItemMove(item, dest, aboveDest);
      _videoClipsM.DataAdapter.IsModified = true;
    }

    protected override string ValidateNewItemName(ITreeItem root, string name) =>
      null;

    protected override void ModelGroupCreate(ITreeItem root, string name) =>
      GroupsM.ItemCreate(name, _videoClipsM.CurrentMediaItem);

    protected override void ModelGroupRename(ITreeGroup group, string name) =>
      GroupsM.ItemRename(group, name);

    protected override void ModelGroupDelete(ITreeGroup group) =>
      GroupsM.ItemDelete(group);

    public override void GroupMove(ITreeGroup group, ITreeGroup dest, bool aboveDest) =>
      GroupsM.GroupMove(group, dest, aboveDest);

    protected override string ValidateNewGroupName(ITreeItem root, string name) =>
      GroupsM.ItemCanRename(name, _videoClipsM.CurrentMediaItem)
        ? null
        : $"{name} group already exists!";

    public override void OnItemSelect(object o) {
      if (o is VideoClipM vc)
        _videoClipsM.SetCurrentVideoClip(vc);
    }
  }
}
