using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MH.Utils.BaseClasses;

public class InterfaceTableDataAdapter<T, TI> : IInterfaceTableDataAdapter<TI> where T : class, TI {
  public event EventHandler<TI> ItemCreatedEvent = delegate { };
  public event EventHandler<TI> ItemUpdatedEvent = delegate { };
  public event EventHandler<TI> ItemDeletedEvent = delegate { };
  public event EventHandler<IList<TI>> ItemsDeletedEvent = delegate { };

  public TableDataAdapter<T> TableDataAdapter { get; }

  public InterfaceTableDataAdapter(TableDataAdapter<T> tableDataAdapter) {
    TableDataAdapter = tableDataAdapter;
  }

  public void AttachEvents() {
    TableDataAdapter.ItemCreatedEvent += (o, e) => ItemCreatedEvent(o, e);
    TableDataAdapter.ItemUpdatedEvent += (o, e) => ItemUpdatedEvent(o, e);
    TableDataAdapter.ItemDeletedEvent += (o, e) => ItemDeletedEvent(o, e);
    TableDataAdapter.ItemsDeletedEvent += (o, e) => ItemsDeletedEvent(o, e.Cast<TI>().ToList());
  }

  public TI GetById(string id, bool nullable = false) =>
    TableDataAdapter.GetById(id, nullable);

  public List<TI> Link(string csv) {
    if (string.IsNullOrEmpty(csv)) return null;

    var items = csv
      .Split(',')
      .Select(x => GetById(x))
      .Where(x => x != null)
      .ToList();

    return items.Count == 0 ? null : items;
  }
}