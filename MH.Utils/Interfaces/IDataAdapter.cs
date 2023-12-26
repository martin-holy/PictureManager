using MH.Utils.BaseClasses;
using System;

namespace MH.Utils.Interfaces;

public interface IDataAdapter {
  public SimpleDB DB { get; set; }
  public string Name { get; }
  public bool IsModified { get; set; }

  public void Load();
  public void Save();
}

public interface ITableDataAdapter : IDataAdapter {
  public int MaxId { get; set; }
  public bool AreTablePropsModified { get; set; }

  public void LoadProps();
  public void SaveProps();
  public void LinkReferences() { }
  public void Clear();
}

public interface IRelationDataAdapter : IDataAdapter { }

public interface ITreeDataAdapter<T> : ITableDataAdapter where T : class, ITreeItem {
  public event EventHandler<ObjectEventArgs<T>> ItemCreatedEvent;
  public T ItemCreate(ITreeItem parent, string name);
  public void ItemRename(ITreeItem item, string name);
  public void ItemCopy(ITreeItem item, ITreeItem dest);
  public void ItemMove(ITreeItem item, ITreeItem dest, bool aboveDest);
  public void ItemDelete(ITreeItem item);
  public void TreeItemDelete(ITreeItem item);
  public string ValidateNewItemName(ITreeItem parent, string name);
}