using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Interfaces;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.CustomControls;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public sealed class FoldersTreeVM : CatTreeViewCategoryBase {
    private readonly Core _core;
    private readonly AppCore _coreVM;

    public FoldersM Model { get; }
    public readonly Dictionary<int, FolderTreeVM> All = new();

    public static RelayCommand<FolderTreeVM> SetAsFolderKeywordCommand { get; } =
      new(item => App.Core.FoldersM.SetAsFolderKeyword(item.Model), item => item != null);

    public FoldersTreeVM(Core core, AppCore coreVM, FoldersM model) : base(Category.Folders, "Folders") {
      _core = core;
      _coreVM = coreVM;
      Model = model;
      CanMoveItem = true;
      CanCopyItem = true;
      IsExpanded = true;

      Model.Items.CollectionChanged += ModelItems_CollectionChanged;
      Model.FolderDeletedEvent += (_, e) => All.Remove(e.Folder.Id);
      
      OnAfterItemRename += (o, e) => {
        // reload if the folder was selected before
        if (o is FolderTreeVM { IsSelected: true } folder)
          _ = _coreVM.TreeView_Select(folder);
      };

      OnAfterItemDelete += (o, _) => {
        // delete folder, sub folders and mediaItems from file system
        if (o is FolderTreeVM folder && Directory.Exists(folder.Model.FullPath))
          AppCore.FileOperationDelete(new() { folder.Model.FullPath }, true, false);
      };
    }

    public void Load() => ModelItems_CollectionChanged(Model.Items, null);

    private void ModelItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
      _coreVM.DrivesTreeVM.SyncCollection((ObservableCollection<ITreeLeaf>)sender, Items, this, SyncCollection);
      UpdateDrivesVisibility();
    }

    private void SyncCollection(ObservableCollection<ITreeLeaf> src, ObservableCollection<ITreeLeaf> dest, ITreeBranch parent, MH.Utils.Tree.OnItemsChanged onItemsChanged) {
      MH.Utils.Tree.SyncCollection<FolderM, FolderTreeVM>(src, dest, parent,
        (model, treeVM) => treeVM.Model.Equals(model),
        model => MH.Utils.Tree.GetDestItem(model, model.Id, All, () => ItemCreateVM(model, parent), onItemsChanged));
    }

    public void UpdateDrivesVisibility() {
      // unHide all hidden
      foreach (var driveTreeVM in _coreVM.DrivesTreeVM.All.Values.Where(x => x.IsHidden))
        driveTreeVM.IsHidden = false;
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

    public void HandleItemExpandedChanged(FolderTreeVM item) {
      if (item is not { IsExpanded: true }) return;
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
          if (_core.ThumbnailsGridsM.Current == null) break;

          var selected = _core.ThumbnailsGridsM.Current.FilteredItems
            .Where(x => x.IsSelected).Select(p => p.FilePath).OrderBy(p => p).ToArray();

          if (selected.SequenceEqual(dragged.OrderBy(x => x)) && dest is FolderTreeVM folder && folder.Model.IsAccessible) return true;

          break;
        }
      }

      return false;
    }

    public override void OnDrop(object src, ICatTreeViewItem dest, bool aboveDest, bool copy) {
      if (dest is not FolderTreeVM destFolder) return;
      var foMode = copy ? FileOperationMode.Copy : FileOperationMode.Move;

      switch (src) {
        case FolderTreeVM srcData: // Folder
          CopyMove(foMode, srcData.Model, destFolder.Model);

          // reload last selected source if was moved
          if (foMode == FileOperationMode.Move && srcData.IsSelected && destFolder.Model.GetByPath(srcData.Model.Name) != null) {
            CatTreeView.ExpandTo(destFolder);
            _ = _coreVM.TreeView_Select(destFolder);
          }

          break;

        case string[]: // MediaItems
          _coreVM.MediaItemsVM.CopyMove(foMode,
            _core.ThumbnailsGridsM.Current.FilteredItems.Where(x => x.IsSelected).ToList(),
            destFolder.Model);
          _core.MediaItemsM.DataAdapter.IsModified = true;

          break;
      }

      _coreVM.MarkUsedKeywordsAndPeople();
    }

    // TODO try to remove dependency to _core
    private void CopyMove(FileOperationMode mode, FolderM srcFolder, FolderM destFolder) {
      var fop = new FileOperationDialog(App.WMain, mode) { PbProgress = { IsIndeterminate = true } };
      fop.RunTask = Task.Run(() => {
        fop.LoadCts = new();
        var token = fop.LoadCts.Token;

        try {
          Model.CopyMove(mode, srcFolder, destFolder, fop.Progress,
            (string srcFilePath, string destFilePath, ref string destFileName) =>
              AppCore.ShowFileOperationCollisionDialog(srcFilePath, destFilePath, fop, ref destFileName), token, _core);
        }
        catch (Exception ex) {
          ErrorDialog.Show(ex);
        }
      }).ContinueWith(_ => _core.RunOnUiThread(() => fop.Close()));

      fop.ShowDialog();
    }

    protected override ICatTreeViewItem ModelItemCreate(ICatTreeViewItem root, string name) =>
      All[Model.ItemCreate((ITreeBranch)ToModel(root), name).Id];

    protected override void ModelItemRename(ICatTreeViewItem item, string name) =>
      Model.ItemRename((FolderM)ToModel(item), name);

    protected override void ModelItemDelete(ICatTreeViewItem item) =>
      Model.ItemDelete((FolderM)ToModel(item));

    protected override string ValidateNewItemName(ICatTreeViewItem root, string name) {
      // check if folder already exists
      if (Directory.Exists(IOExtensions.PathCombine(((FolderM)ToModel(root)).FullPath, name)))
        return "Folder already exists!";

      // check if is correct folder name
      if (Path.GetInvalidPathChars().Any(name.Contains))
        return "New folder's name contains incorrect character(s)!";

      return null;
    }

    public override string GetTitle(object item) =>
      (item as FolderTreeVM)?.Model.Name;

    private static object ToModel(object item) =>
      item switch {
        FolderTreeVM x => x.Model,
        FoldersTreeVM x => x.Model,
        _ => null
      };
  }
}
