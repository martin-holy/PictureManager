using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Extensions;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public sealed class FoldersTreeVM : BaseCatTreeViewCategory {
    private readonly Core _core;

    public FoldersM Model { get; }
    public readonly Dictionary<int, FolderTreeVM> All = new();

    public FoldersTreeVM(Core core, FoldersM model) : base(Category.Folders) {
      _core = core;
      Model = model;

      Title = "Folders";
      IconName = IconName.Folder;
      CanHaveSubItems = true;
      CanCreateItems = true;
      CanRenameItems = true;
      CanDeleteItems = true;
      CanMoveItem = true;
      CanCopyItem = true;
      IsExpanded = true;

      Model.Items.CollectionChanged += ModelItems_CollectionChanged;
      Model.FolderDeletedEvent += (_, e) => All.Remove(e.Folder.Id);

      // load items
      ModelItems_CollectionChanged(Model.Items, null);
    }

    private void ModelItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
      SyncCollection((ObservableCollection<ITreeLeaf>)sender, Items, this, SyncCollection);
      UpdateDrivesVisibility();
    }

    private void SyncCollection(ObservableCollection<ITreeLeaf> src, ObservableCollection<ITreeLeaf> dest, ITreeBranch parent, Domain.Utils.Tree.OnItemsChanged onItemsChanged) {
      Domain.Utils.Tree.SyncCollection<FolderM, FolderTreeVM>(src, dest, parent,
        (model, treeVM) => treeVM.Model.Equals(model),
        model => Domain.Utils.Tree.GetDestItem(model, model.Id, All, () => ItemCreateVM(model, parent), onItemsChanged));
    }

    public void UpdateDrivesVisibility() {
      // unHide all hidden
      foreach (var folderTreeVM in All.Values.Where(x => x.IsHidden))
        folderTreeVM.IsHidden = false;

      UpdateItemsVisibility(Items.Cast<FolderTreeVM>());

      // collapse all drives
      foreach (var item in Items.Cast<FolderTreeVM>())
        item.IsExpanded = false;
    }

    private void UpdateItemsVisibility(IEnumerable<FolderTreeVM> items) {
      foreach (var item in items)
        if (!_core.FoldersM.IsFolderVisible(item.Model))
          item.IsHidden = true;
    }

    private FolderTreeVM ItemCreateVM(FolderM model, ITreeBranch parent) {
      var item = new FolderTreeVM(model, parent);
      item.OnExpandedChanged += (o, _) => HandleItemExpandedChanged(o as FolderTreeVM);

      return item;
    }

    private void HandleItemExpandedChanged(FolderTreeVM item) {
      if (item == null) return;
      item.UpdateIconName();
      if (!item.IsExpanded) return;
      item.Model.LoadSubFolders(false);
      UpdateItemsVisibility(item.Items.Cast<FolderTreeVM>());
    }

    public override bool CanDrop(object src, ICatTreeViewItem dest) {
      switch (src) {
        case FolderTreeVM srcData: { // Folder
          if (dest is FolderTreeVM destData && !destData.Model.HasThisParent(srcData.Model) && !Equals(srcData, destData) &&
              destData.Model.IsAccessible && !Equals((FolderTreeVM)srcData.Parent, destData)) return true;

          break;
        }
        case string[] dragged: { // MediaItems
          if (_core.MediaItems.ThumbsGrid == null) break;

          var selected = _core.MediaItems.ThumbsGrid.FilteredItems
            .Where(x => x.IsSelected).Select(p => p.FilePath).OrderBy(p => p).ToArray();

          if (selected.SequenceEqual(dragged.OrderBy(x => x)) && dest is FolderTreeVM folder && folder.Model.IsAccessible) return true;

          break;
        }
      }

      return false;
    }

    public override void OnDrop(object src, ICatTreeViewItem dest, bool aboveDest, bool copy) {
      // handled in OnAfterOnDrop (TreeViewCategories)
    }

    public override string GetTitle(ICatTreeViewItem item) => TreeToModel(item).Name;

    public override bool CanCreateItem(ICatTreeViewItem item) => item is FolderTreeVM;

    public override bool CanRenameItem(ICatTreeViewItem item) => item is FolderTreeVM && item.Parent is not ICatTreeViewCategory;

    public override bool CanDeleteItem(ICatTreeViewItem item) => item is FolderTreeVM && item.Parent is not ICatTreeViewCategory;

    public override bool CanSort(ICatTreeViewItem root) => false;

    public override string ValidateNewItemTitle(ICatTreeViewItem root, string name) {
      // check if folder already exists
      if (Directory.Exists(Extension.PathCombine(((FolderTreeVM)root).Model.FullPath, name)))
        return "Folder already exists!";

      // check if is correct folder name
      if (Path.GetInvalidPathChars().Any(name.Contains))
        return "New folder's name contains incorrect character(s)!";

      return null;
    }

    public override ICatTreeViewItem ItemCreate(ICatTreeViewItem root, string name) {
      root.IsExpanded = true;
      var folderM = _core.FoldersM.ItemCreate(ToTreeBranch(root), name);
      return All[folderM.Id];
    }

    public override void ItemRename(ICatTreeViewItem item, string name) =>
      _core.FoldersM.ItemRename(TreeToModel(item), name);

    public override void ItemDelete(ICatTreeViewItem item) {
      _core.FoldersM.ItemDelete(TreeToModel(item));

      // collapse parent if doesn't have any sub folders
      if (item.Parent.Items.Count == 0)
        ((ICatTreeViewItem)item.Parent).IsExpanded = false;
    }

    public static void CopyMove(FileOperationMode mode, FolderM srcFolder, FolderM destFolder) {
      var fop = new FileOperationDialog(App.WMain, mode) { PbProgress = { IsIndeterminate = true } };
      fop.RunTask = Task.Run(() => {
        fop.LoadCts = new();
        var token = fop.LoadCts.Token;

        try {
          FoldersM.CopyMove(mode, srcFolder, destFolder, fop.Progress,
            (string srcFilePath, string destFilePath, ref string destFileName) =>
              AppCore.ShowFileOperationCollisionDialog(srcFilePath, destFilePath, fop, ref destFileName), token);
        }
        catch (Exception ex) {
          ErrorDialog.Show(ex);
        }
      }).ContinueWith(_ => App.Core.RunOnUiThread(() => fop.Close()));

      fop.ShowDialog();
    }

    private static FolderM TreeToModel(object item) => ((FolderTreeVM)item).Model;

    private static ITreeBranch ToTreeBranch(object item) =>
      item switch {
        FolderTreeVM x => x.Model,
        FoldersTreeVM x => x.Model,
        _ => null
      };
  }
}
