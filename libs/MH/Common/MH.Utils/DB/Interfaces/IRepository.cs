using System;
using System.Collections.Generic;

namespace MH.Utils.DB.Interfaces;

public interface IRepository {
  public bool IsModified { get; set; }
}

public interface IRepository<T> : IRepository {
  public event EventHandler<T> ItemCreatedEvent;
  public event EventHandler<T> ItemUpdatedEvent;
  public event EventHandler<T> ItemDeletedEvent;
  public event EventHandler<IList<T>> ItemsDeletedEvent;
}