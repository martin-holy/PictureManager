using MH.UI.Controls;
using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.EventsArgs;
using MH.Utils.Interfaces;
using System;

namespace MH.UI.BaseClasses;

public class TreeCategory : TreeItem, ITreeCategory {
  public int Id { get; }
  public bool CanCopyItem { get; set; }
  public bool CanMoveItem { get; set; }
  public bool UseTreeDelete { get; set; }
  public TreeView<ITreeItem> TreeView { get; } = new();

  public static RelayCommand<ITreeItem> ItemCreateCommand { get; } = new(
    item => GetCategory(item)?.ItemCreate(item), null, "New");

  public static RelayCommand<ITreeItem> ItemRenameCommand { get; } = new(
    item => GetCategory(item)?.ItemRename(item), null, "Rename");

  public static RelayCommand<ITreeItem> ItemDeleteCommand { get; } = new(
    item => GetCategory(item)?.ItemDelete(item), null, "Delete");

  public static RelayCommand<ITreeCategory> GroupCreateCommand { get; } = new(
    item => GetCategory(item)?.GroupCreate(item), null, "New Group");

  public static RelayCommand<ITreeGroup> GroupRenameCommand { get; } = new(
    item => GetCategory(item)?.GroupRename(item), null, "Rename Group");

  public static RelayCommand<ITreeGroup> GroupDeleteCommand { get; } = new(
    item => GetCategory(item)?.GroupDelete(item), null, "Delete Group");

  public TreeCategory(string icon, string name, int id) : base(icon, name) {
    Id = id;
    TreeView.RootHolder.Add(this);
    TreeView.TreeItemSelectedEvent += (_, e) => OnItemSelected(e.Data);
  }

  public virtual void ItemCreate(ITreeItem parent) => throw new NotImplementedException();
  public virtual void ItemRename(ITreeItem item) => throw new NotImplementedException();
  public virtual void ItemDelete(ITreeItem item) => throw new NotImplementedException();
  public virtual void GroupCreate(ITreeItem parent) => throw new NotImplementedException();
  public virtual void GroupRename(ITreeGroup group) => throw new NotImplementedException();
  public virtual void GroupDelete(ITreeGroup group) => throw new NotImplementedException();
  public virtual void GroupMove(ITreeGroup group, ITreeGroup dest, bool aboveDest) => throw new NotImplementedException();

  public virtual void OnItemSelected(object item) { }

  public virtual bool CanDrop(object src, ITreeItem dest) =>
    CanDrop(src as ITreeItem, dest);

  public virtual void OnDrop(object src, ITreeItem dest, bool aboveDest, bool copy) =>
    throw new NotImplementedException();

  public static bool CanDrop(ITreeItem src, ITreeItem dest) {
    if (src == null || dest == null || Equals(src, dest) ||
        src.Parent.Equals(dest) || dest.Parent?.Equals(src) == true ||
        (src is ITreeGroup && dest is not ITreeGroup)) return false;

    // if src or dest categories are null or they are not equal
    if (Tree.GetParentOf<ITreeCategory>(src) is not { } srcCat ||
        Tree.GetParentOf<ITreeCategory>(dest) is not { } destCat ||
        !Equals(srcCat, destCat)) return false;

    return true;
  }

  public static bool GetNewName(bool forItem, string oldName, out string newName, ITreeItem item, Func<ITreeItem, string, string> validator, string icon) {
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
    var result = Dialog.Show(inputDialog);
    newName = inputDialog.Answer;

    return result == 1;
  }

  private static ITreeCategory GetCategory(ITreeItem item) =>
    Tree.GetParentOf<ITreeCategory>(item);
}

public class TreeCategory<TI> : TreeCategory where TI : class, ITreeItem {
  protected ITreeDataAdapter<TI> DataAdapter { get; set; }

  public event EventHandler<TreeItemDroppedEventArgs> AfterDropEvent = delegate { };

  public TreeCategory(string icon, string name, int id) : base(icon, name, id) { }

  public override void ItemCreate(ITreeItem parent) {
    if (!GetNewName(true, string.Empty, out var newName, parent, DataAdapter.ValidateNewItemName, Icon)) return;

    try {
      parent.IsExpanded = true;
      DataAdapter.ItemCreate(parent, newName);
    }
    catch (Exception ex) {
      Log.Error(ex);
    }
  }

  public override void ItemRename(ITreeItem item) {
    if (!GetNewName(true, item.Name, out var newName, item, DataAdapter.ValidateNewItemName, Icon)) return;

    try {
      DataAdapter.ItemRename(item, newName);
    }
    catch (Exception ex) {
      Log.Error(ex);
    }
  }

  public override void ItemDelete(ITreeItem item) {
    if (!DeleteAccepted(item.Name)) return;

    try {
      if (UseTreeDelete)
        DataAdapter.TreeItemDelete(item);
      else
        DataAdapter.ItemDelete(item);

      // collapse parent if doesn't have any sub items
      if (item.Parent is { } parent && parent.Items.Count == 0)
        parent.IsExpanded = false;
    }
    catch (Exception ex) {
      Log.Error(ex);
    }
  }

  public override void OnDrop(object src, ITreeItem dest, bool aboveDest, bool copy) {
    // groups
    if (src is ITreeGroup srcGroup && dest is ITreeGroup destGroup) {
      GroupMove(srcGroup, destGroup, aboveDest);
      return;
    }

    // items
    if (src is ITreeItem srcItem && dest != null) {
      if (copy)
        DataAdapter.ItemCopy(srcItem, dest);
      else
        DataAdapter.ItemMove(srcItem, dest, aboveDest);
    }

    AfterDropEvent(this, new(src, dest, aboveDest, copy));
  }

  protected static bool DeleteAccepted(string name) =>
    Dialog.Show(new MessageDialog(
      "Delete Confirmation",
      $"Do you really want to delete '{name}'?",
      "IconQuestion",
      true)) == 1;
}

public class TreeCategory<TI, TG> : TreeCategory<TI>, ITreeCategory<TG> where TI : class, ITreeItem where TG : class, ITreeItem  {
  protected ITreeDataAdapter<TG> GroupDataAdapter { get; set; }

  public TreeCategory(string icon, string name, int id) : base(icon, name, id) { }

  public override void GroupCreate(ITreeItem parent) {
    if (!GetNewName(false, string.Empty, out var newName, parent, GroupDataAdapter.ValidateNewItemName, Icon)) return;
    
    GroupDataAdapter.ItemCreate(parent, newName);
  }

  public override void GroupRename(ITreeGroup group) {
    if (!GetNewName(false, group.Name, out var newName, group, GroupDataAdapter.ValidateNewItemName, Icon)) return;

    GroupDataAdapter.ItemRename(group, newName);
  }

  public override void GroupMove(ITreeGroup group, ITreeGroup dest, bool aboveDest) =>
    GroupDataAdapter.ItemMove(group, dest, aboveDest);

  public override void GroupDelete(ITreeGroup group) {
    if (!DeleteAccepted(group.Name)) return;
    
    GroupDataAdapter.ItemDelete(group);
  }

  public void SetGroupDataAdapter(ITreeDataAdapter<TG> gda) =>
    GroupDataAdapter = gda;
}