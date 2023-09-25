using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MH.Utils.BaseClasses;

public class TreeDataAdapter<T> : DataAdapter<T>, ITreeDataAdapter<T> where T : class, ITreeItem {
  public event EventHandler<ObjectEventArgs<T>> ItemRenamedEvent = delegate { };

  public TreeDataAdapter(string tableName, int propsCount) : base(tableName, propsCount) { }

  public virtual T ItemCreate(ITreeItem parent, string name) => throw new NotImplementedException();
  public virtual void ItemCopy(ITreeItem item, ITreeItem dest) => throw new NotImplementedException();
  
  protected void RaiseItemRenamed(T item) => ItemRenamedEvent(this, new(item));

  protected virtual void OnItemRenamed(T item) { }

  public virtual T TreeItemCreate(T item) {
    Tree.SetInOrder(item.Parent.Items, item, x => x.Name);
    All.Add(item);
    RaiseItemCreated(item);
    OnItemCreated(item);
    return item;
  }

  public virtual void ItemRename(ITreeItem item, string name) {
    item.Name = name;
    Tree.SetInOrder(item.Parent.Items, item, x => x.Name);
    IsModified = true;
    RaiseItemRenamed((T)item);
    OnItemRenamed((T)item);
  }

  public virtual string ValidateNewItemName(ITreeItem parent, string name) =>
    All.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
      ? $"{name} item already exists!"
      : null;

  public virtual void ItemMove(ITreeItem item, ITreeItem dest, bool aboveDest) {
    Tree.ItemMove(item, dest, aboveDest);
    IsModified = true;
  }

  public virtual void ItemDelete(ITreeItem item) {
    All.Remove((T)item);
    IsModified = true;
    RaiseItemDeleted((T)item);
    OnItemDeleted((T)item);
  }

  public virtual void TreeItemDelete(T item) {
    var items = new List<T>();
    Tree.GetThisAndItemsRecursive(item, ref items);

    foreach (var treeItem in items)
      ItemDelete(treeItem);

    RaiseItemsDeleted(items);
    OnItemsDeleted(items);
  }

  protected override void OnItemsDeleted(IList<T> items) {
    items[0].Parent?.Items.Remove(items[0]);

    foreach (var item in items) {
      item.Parent = null;
      item.Items.Clear();
    }
  }
}