using System;
using System.Collections.Generic;
using System.Linq;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.BaseClasses;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class ViewersM : TreeCategoryBase {
    private ViewerM _current;

    public DataAdapter DataAdapter { get; set; }
    public List<ViewerM> All { get; } = new();
    public ViewerM Current { get => _current; set { _current = value; OnPropertyChanged(); } }

    public RelayCommand<ViewerM> SetCurrentCommand { get; }

    public ViewersM() : base(Res.IconEye, Category.Viewers, "Viewers") {
      SetCurrentCommand = new(SetCurrent);
    }

    protected override ITreeItem ModelItemCreate(ITreeItem root, string name) {
      var item = new ViewerM(DataAdapter.GetNextId(), name, root);
      root.Items.SetInOrder(item, x => x.Name);
      All.Add(item);

      return item;
    }

    protected override void ModelItemRename(ITreeItem item, string name) {
      item.Name = name;
      item.Parent.Items.SetInOrder(item, x => x.Name);
      DataAdapter.IsModified = true;
    }

    protected override void ModelItemDelete(ITreeItem item) {
      var viewer = (ViewerM)item;
      viewer.Parent.Items.Remove(viewer);
      viewer.Parent = null;
      viewer.IncludedFolders.Clear();
      viewer.ExcludedFolders.Clear();
      viewer.ExcludedKeywords.Clear();
      All.Remove(viewer);
      DataAdapter.IsModified = true;
    }

    protected override string ValidateNewItemName(ITreeItem root, string name) =>
      All.Any(x => x.Name.Equals(name, StringComparison.CurrentCulture))
        ? $"{name} item already exists!"
        : null;

    public void ToggleCategoryGroup(ViewerM viewer, int groupId) {
      viewer.ExcCatGroupsIds.Toggle(groupId);
      DataAdapter.IsModified = true;
    }

    public void AddFolder(ViewerM viewer, FolderM folder, bool included) {
      (included ? viewer.IncludedFolders : viewer.ExcludedFolders).SetInOrder(folder, x => x.FullPath);
      DataAdapter.IsModified = true;
    }

    public void RemoveFolder(ViewerM viewer, FolderM folder, bool included) {
      (included ? viewer.IncludedFolders : viewer.ExcludedFolders).Remove(folder);
      DataAdapter.IsModified = true;
    }

    public void AddKeyword(ViewerM viewer, KeywordM keyword) {
      viewer.ExcludedKeywords.SetInOrder(keyword, x => x.FullName);
      DataAdapter.IsModified = true;
    }

    public void RemoveKeyword(ViewerM viewer, KeywordM keyword) {
      viewer.ExcludedKeywords.Remove(keyword);
      DataAdapter.IsModified = true;
    }

    public void SetCurrent(ViewerM viewer) {
      if (Current != null && Current == viewer) {
        Current.UpdateHashSets();
        OnPropertyChanged(nameof(Current));
        return;
      }

      var save = false;

      if (Current != null) {
        Current.IsDefault = false;
        save = true;
      }

      if (viewer != null) {
        viewer.IsDefault = true;
        save = true;
        viewer.UpdateHashSets();
        Current = viewer;
      }
      else
        Current = null;

      if (save)
        DataAdapter.Save();
    }

    public bool CanViewerSee(FolderM folder) =>
      Current?.CanSee(folder) != false;

    public bool CanViewerSeeContentOf(FolderM folder) =>
      Current?.CanSeeContentOf(folder) != false;

    public bool CanViewerSee(MediaItemM mediaItem) =>
      Current?.CanSee(mediaItem) != false;
  }
}
