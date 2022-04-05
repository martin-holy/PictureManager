using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public sealed class DrivesTreeVM {
    public readonly Dictionary<int, DriveTreeVM> All = new();
    public event EventHandler<ObjectEventArgs<FolderTreeVM>> DriveExpandedChangedEventHandler = delegate { };

    public void SyncCollection(ObservableCollection<ITreeLeaf> src, ObservableCollection<ITreeLeaf> dest, ITreeBranch parent, MH.Utils.Tree.OnItemsChanged onItemsChanged) {
      MH.Utils.Tree.SyncCollection<FolderM, DriveTreeVM>(src, dest, parent,
        (model, treeVM) => treeVM.Model.Equals(model),
        model => MH.Utils.Tree.GetDestItem(model, model.Id, All, () => ItemCreateVM(model, parent), onItemsChanged));
    }

    private DriveTreeVM ItemCreateVM(FolderM model, ITreeBranch parent) {
      var item = new DriveTreeVM(model, parent);
      item.OnExpandedChanged += (o, _) =>
        DriveExpandedChangedEventHandler(this, new(o as FolderTreeVM));

      return item;
    }
  }
}
