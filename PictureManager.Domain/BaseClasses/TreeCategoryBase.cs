﻿using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Interfaces;
using System;

namespace PictureManager.Domain.BaseClasses {
  public class TreeCategoryBase : TreeCategory {
    public Category Category { get; }
    public TreeView<ITreeItem> TreeView { get; } = new();

    public event EventHandler<ObjectEventArgs<ITreeItem>> AfterItemCreateEventHandler = delegate { };
    public event EventHandler<ObjectEventArgs<ITreeItem>> AfterItemRenameEventHandler = delegate { };
    public event EventHandler<ObjectEventArgs<ITreeItem>> AfterItemDeleteEventHandler = delegate { };

    public TreeCategoryBase(string icon, Category category, string name) : base(icon, name) {
      TreeView.RootHolder.Add(this);
      // TODO use OnItemSelect override or TreeItemSelectedEvent in derived classes?
      TreeView.TreeItemSelectedEvent += (_, e) => OnItemSelect(e.Data);
      Category = category;
    }

    protected virtual string ValidateNewItemName(ITreeItem root, string name) => throw new NotImplementedException();
    protected virtual ITreeItem ModelItemCreate(ITreeItem root, string name) => throw new NotImplementedException();
    protected virtual void ModelItemRename(ITreeItem item, string name) => throw new NotImplementedException();
    protected virtual void ModelItemDelete(ITreeItem item) => throw new NotImplementedException();
    
    protected virtual string ValidateNewGroupName(ITreeItem root, string name) => throw new NotImplementedException();
    protected virtual void ModelGroupCreate(ITreeItem root, string name) => throw new NotImplementedException();
    protected virtual void ModelGroupRename(ITreeGroup group, string name) => throw new NotImplementedException();
    protected virtual void ModelGroupDelete(ITreeGroup group) => throw new NotImplementedException();

    public virtual void OnItemSelect(object item) { }

    public override void ItemCreate(ITreeItem root) {
      if (!GetNewName(true, string.Empty, out var newName,
        root, ValidateNewItemName, Res.CategoryToIcon(Category))) return;

      try {
        root.IsExpanded = true;
        var item = ModelItemCreate(root, newName);
        AfterItemCreateEventHandler(this, new(item));
      }
      catch (Exception ex) {
        Log.Error(ex);
      }
    }

    public override void ItemRename(ITreeItem item) {
      if (!GetNewName(true, item.Name, out var newName,
        item, ValidateNewItemName, Res.CategoryToIcon(Category))) return;

      try {
        ModelItemRename(item, newName);
        AfterItemRenameEventHandler(this, new(item));
      }
      catch (Exception ex) {
        Log.Error(ex);
      }
    }

    public override void ItemDelete(ITreeItem item) {
      if (!DeleteAccepted(item.Name)) return;

      try {
        ModelItemDelete(item);

        // collapse parent if doesn't have any sub items
        if (item.Parent is { } parent && parent.Items.Count == 0)
          parent.IsExpanded = false;

        AfterItemDeleteEventHandler(this, new(item));
      }
      catch (Exception ex) {
        Log.Error(ex);
      }
    }

    public override void GroupCreate(ITreeItem root) {
      if (!GetNewName(false, string.Empty, out var newName,
        root, ValidateNewGroupName, Res.CategoryToIcon(Category))) return;

      ModelGroupCreate(root, newName);
    }

    public override void GroupRename(ITreeGroup group) {
      if (!GetNewName(false, group.Name, out var newName,
        group, ValidateNewGroupName, Res.CategoryToIcon(Category))) return;

      ModelGroupRename(group, newName);
    }

    public override void GroupDelete(ITreeGroup group) {
      if (!DeleteAccepted(group.Name)) return;

      ModelGroupDelete(group);
    }

    private static bool GetNewName(bool forItem, string oldName, out string newName, ITreeItem item, Func<ITreeItem, string, string> validator, string icon) {
      var action = string.IsNullOrEmpty(oldName) ? "New" : "Rename";
      var target = forItem ? "Item" : "Group";
      var question = string.IsNullOrEmpty(oldName)
        ? $"Enter the name of the new {target}."
        : $"Enter the new name for the {target}.";
      var inputDialog = new InputDialog(
        $"{action} {target}",
        question,
        icon,
        oldName,
        answer => validator(item, answer));
      var result = Core.DialogHostShow(inputDialog);
      newName = inputDialog.Answer;

      return result == 1;
    }

    private static bool DeleteAccepted(string name) =>
      Core.DialogHostShow(new MessageDialog(
        "Delete Confirmation",
        $"Do you really want to delete '{name}'?",
        Res.IconQuestion,
        true)) == 1;
  }
}
