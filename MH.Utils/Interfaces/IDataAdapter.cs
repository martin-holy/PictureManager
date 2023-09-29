using MH.Utils.BaseClasses;
using System;
using System.Collections.Generic;

namespace MH.Utils.Interfaces; 

public interface IDataAdapter {
  public SimpleDB DB { get; set; }
  public string TableName { get; }
  public int MaxId { get; set; }
    
  public bool IsModified { get; set; }
  public bool AreTablePropsModified { get; set; }

  public void Load();
  public void Save();
  public void LoadProps();
  public void SaveProps();
  public void LinkReferences() { }
  public void Clear();
}

public interface IDataAdapter<T> : IDataAdapter where T : class {
  public Dictionary<int, T> AllDict { get; set; }
  public HashSet<T> All { get; set; }

  public event EventHandler<ObjectEventArgs<T>> ItemCreatedEvent;
  public event EventHandler<ObjectEventArgs<T>> ItemDeletedEvent;
  public event EventHandler<ObjectEventArgs<IList<T>>> ItemsDeletedEvent;
}

public interface ITreeDataAdapter<T> : IDataAdapter<T> where T : class, ITreeItem {
  public event EventHandler<ObjectEventArgs<T>> ItemRenamedEvent;

  public T ItemCreate(ITreeItem parent, string name);
  public void ItemRename(ITreeItem item, string name);
  public void ItemCopy(ITreeItem item, ITreeItem dest);
  public void ItemMove(ITreeItem item, ITreeItem dest, bool aboveDest);
  public void ItemDelete(ITreeItem item);
  public void TreeItemDelete(ITreeItem item);
  public string ValidateNewItemName(ITreeItem parent, string name);
}