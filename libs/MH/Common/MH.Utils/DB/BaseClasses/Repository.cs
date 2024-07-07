using MH.Utils.DB.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MH.Utils.DB.BaseClasses;

public class Repository : IRepository {
  public bool IsModified { get; set; }
}

public class Repository<T> : Repository {
  public event EventHandler<T> ItemCreatedEvent = delegate { };
  public event EventHandler<T> ItemUpdatedEvent = delegate { };
  public event EventHandler<T> ItemDeletedEvent = delegate { };
  public event EventHandler<IList<T>> ItemsDeletedEvent = delegate { };

  protected virtual void OnItemCreate(T item) { }
  protected virtual void OnItemUpdate(T item) { }
  protected virtual void OnItemDelete(T item) { }
  protected virtual void OnItemsDelete(IList<T> items) { }

  protected virtual void RaiseItemCreated(T item) => ItemCreatedEvent(this, item);
  protected virtual void RaiseItemUpdated(T item) => ItemUpdatedEvent(this, item);
  protected virtual void RaiseItemDeleted(T item) => ItemDeletedEvent(this, item);
  protected virtual void RaiseItemsDeleted(IList<T> items) => ItemsDeletedEvent(this, items);

  protected virtual void OnItemCreated(T item) { }
  protected virtual void OnItemUpdated(T item) { }
  protected virtual void OnItemDeleted(T item) { }
  protected virtual void OnItemsDeleted(IList<T> items) { }

  public virtual T ItemCreate(T item) {
    OnItemCreate(item);
    IsModified = true;
    RaiseItemCreated(item);
    OnItemCreated(item);
    return item;
  }

  public virtual void ItemDelete(T item, bool singleDelete = true) {
    if (singleDelete) {
      ItemsDelete(new[] { item });
      return;
    }

    OnItemDelete(item);
    IsModified = true;
    RaiseItemDeleted(item);
  }

  public virtual void ItemsDelete(IList<T>? items) {
    if (items == null || items.Count == 0) return;
    foreach (var item in items) ItemDelete(item, false);
    RaiseItemsDeleted(items);
    OnItemsDeleted(items);
    foreach (var item in items) OnItemDeleted(item);
  }
}

public class Repository<T, TI> : Repository<T>, IRepository<TI> {
  private event EventHandler<TI> ItemCreatedIEvent = delegate { };
  private event EventHandler<TI> ItemUpdatedIEvent = delegate { };
  private event EventHandler<TI> ItemDeletedIEvent = delegate { };
  private event EventHandler<IList<TI>> ItemsDeletedIEvent = delegate { };

  event EventHandler<TI> IRepository<TI>.ItemCreatedEvent {
    add => ItemCreatedIEvent += value;
    remove => ItemCreatedIEvent -= value;
  }

  event EventHandler<TI> IRepository<TI>.ItemUpdatedEvent {
    add => ItemUpdatedIEvent += value;
    remove => ItemUpdatedIEvent -= value;
  }

  event EventHandler<TI> IRepository<TI>.ItemDeletedEvent {
    add => ItemDeletedIEvent += value;
    remove => ItemDeletedIEvent -= value;
  }

  event EventHandler<IList<TI>> IRepository<TI>.ItemsDeletedEvent {
    add => ItemsDeletedIEvent += value;
    remove => ItemsDeletedIEvent -= value;
  }

  protected override void RaiseItemCreated(T item) {
    base.RaiseItemCreated(item);
    ItemCreatedIEvent(this, (TI)(object)item!);
  }

  protected override void RaiseItemUpdated(T item) {
    base.RaiseItemUpdated(item);
    ItemUpdatedIEvent(this, (TI)(object)item!);
  }

  protected override void RaiseItemDeleted(T item) {
    base.RaiseItemDeleted(item);
    ItemDeletedIEvent(this, (TI)(object)item!);
  }

  protected override void RaiseItemsDeleted(IList<T> items) {
    base.RaiseItemsDeleted(items);
    ItemsDeletedIEvent(this, items.Cast<TI>().ToList());
  }
}