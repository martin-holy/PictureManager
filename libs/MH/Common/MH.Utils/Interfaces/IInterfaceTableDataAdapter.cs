using System;
using System.Collections.Generic;

namespace MH.Utils.Interfaces;

public interface IInterfaceTableDataAdapter<T> {
  public event EventHandler<T> ItemCreatedEvent;
  public event EventHandler<T> ItemUpdatedEvent;
  public event EventHandler<T> ItemDeletedEvent;
  public event EventHandler<IList<T>> ItemsDeletedEvent;

  public T GetById(string id, bool nullable = false);
  public List<T> Link(string csv);
}