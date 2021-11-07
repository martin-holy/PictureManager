﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using MH.UI.WPF.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public sealed class FolderKeywordsTreeVM : CatTreeViewCategoryBase {
    private readonly Core _core;

    public FolderKeywordsM Model { get; }
    public readonly Dictionary<int, FolderKeywordTreeVM> All = new();
    public static RelayCommand<object> OpenFolderKeywordsListCommand { get; } = new(FolderKeywordList.Open);

    public FolderKeywordsTreeVM(Core core, FolderKeywordsM model) : base(Category.FolderKeywords, "Folder Keywords") {
      _core = core;
      Model = model;

      Model.Items.CollectionChanged += ModelItems_CollectionChanged;
      Model.ReloadEvent += (_, _) => {
        All.Clear();
        LoadRoot();
      };

      LoadRoot();
    }

    private void ModelItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
      SyncCollection((ObservableCollection<ITreeLeaf>)sender, Items, this, SyncCollection);

    private void SyncCollection(ObservableCollection<ITreeLeaf> src, ObservableCollection<ITreeLeaf> dest, ITreeBranch parent, MH.Utils.Tree.OnItemsChanged onItemsChanged) {
      MH.Utils.Tree.SyncCollection<FolderKeywordM, FolderKeywordTreeVM>(src, dest, parent,
        (model, treeVM) => treeVM.Model.Equals(model),
        model => MH.Utils.Tree.GetDestItem(model, model.Id, All, () => ItemCreateVM(model, parent), onItemsChanged));
    }

    private void LoadRoot() {
      ModelItems_CollectionChanged(Model.Items, null);
      UpdateItemsVisibility(Items.Cast<FolderKeywordTreeVM>());
    }

    private void UpdateItemsVisibility(IEnumerable<FolderKeywordTreeVM> items) {
      foreach (var item in items)
        if (item.Model.Folders.All(x => !_core.FoldersM.IsFolderVisible(x)))
          item.IsHidden = true;
    }

    private FolderKeywordTreeVM ItemCreateVM(FolderKeywordM model, ITreeBranch parent) {
      var item = new FolderKeywordTreeVM(model, parent);
      item.OnExpandedChanged += (o, _) => HandleItemExpandedChanged(o as FolderKeywordTreeVM);

      return item;
    }

    private void HandleItemExpandedChanged(FolderKeywordTreeVM item) {
      if (item is not { IsExpanded: true }) return;

      foreach (var folder in item.Model.Folders)
        folder.LoadSubFolders(false);

      UpdateItemsVisibility(item.Items.Cast<FolderKeywordTreeVM>());
    }
  }
}
