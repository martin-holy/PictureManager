using System;
using System.Linq;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.BaseClasses;
using PictureManager.Domain.DataAdapters;

namespace PictureManager.Domain.Models {
  public sealed class ViewersM : TreeCategoryBase {
    private readonly Core _core;
    private ViewerM _current;
    private ViewerM _selected;

    public HeaderedListItem<object, string> ViewerMainTabsItem { get; set; }
    public ViewersDataAdapter DataAdapter { get; set; }
    public ViewerM Current { get => _current; set { _current = value; OnPropertyChanged(); } }
    public ViewerM Selected {
      get => _selected;
      set {
        _selected = value;
        OnPropertyChanged();
        value.Reload(_core.CategoryGroupsM.DataAdapter.All.Values);
      }
    }

    public RelayCommand<ViewerM> SetCurrentCommand { get; }
    public RelayCommand<ViewerM> UpdateExcCatGroupsIdsCommand { get; }

    public ViewersM(Core core) : base(Res.IconEye, Category.Viewers, "Viewers") {
      _core = core;
      SetCurrentCommand = new(SetCurrent);
      UpdateExcCatGroupsIdsCommand = new(UpdateExcCatGroupsIds);
    }

    protected override ITreeItem ModelItemCreate(ITreeItem root, string name) {
      var item = new ViewerM(DataAdapter.GetNextId(), name, root);
      root.Items.SetInOrder(item, x => x.Name);
      DataAdapter.All.Add(item.Id, item);

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
      DataAdapter.All.Remove(viewer.Id);
      DataAdapter.IsModified = true;
    }

    protected override string ValidateNewItemName(ITreeItem root, string name) =>
      DataAdapter.All.Values.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        ? $"{name} item already exists!"
        : null;

    private void UpdateExcCatGroupsIds() {
      Selected.UpdateExcCatGroupsIds();
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
      if (Current != null)
        Current.IsDefault = false;

      if (viewer != null)
        viewer.IsDefault = true;
      else
        Current = null;

      DataAdapter.Save();
      DataAdapter.DB.SaveAllTables();
      DataAdapter.DB.LoadAllTables(null);
      DataAdapter.DB.LinkReferences(null);
      DataAdapter.DB.ClearDataAdapters();
    }

    public bool CanViewerSee(FolderM folder) =>
      Current?.CanSee(folder) != false;

    public bool CanViewerSeeContentOf(FolderM folder) =>
      Current?.CanSeeContentOf(folder) != false;

    public bool CanViewerSee(MediaItemM mediaItem) =>
      Current?.CanSee(mediaItem) != false;
  }
}
