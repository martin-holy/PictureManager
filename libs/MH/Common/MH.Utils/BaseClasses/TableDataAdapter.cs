﻿using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MH.Utils.BaseClasses;

public class TableDataAdapter<T> : DataAdapter<T>, ITableDataAdapter where T : class {
  private bool _areTablePropsModified;
  private Dictionary<int, int> _notFoundIds;

  public bool AreTablePropsModified {
    get => _areTablePropsModified;
    set {
      _areTablePropsModified = value;
      if (value)
        DB.AddChange();
    }
  }

  public string TablePropsFilePath { get; }
  public Dictionary<string, string> TableProps { get; } = new();
  public Dictionary<int, T> AllDict { get; set; }
  public List<(T, string[])> AllCsv { get; set; }

  public TableDataAdapter(SimpleDB db, string name, int propsCount) : base(db, name, propsCount) {
    TablePropsFilePath = Path.Combine("db", $"{name}_props.csv");
  }

  public virtual void PropsToCsv() { }
  public virtual void LinkReferences() { }

  public override void Load() {
    AllDict = new();
    AllCsv = new();
    base.Load();
  }

  public void Clear() {
    All = AllDict.Values.ToHashSet();
    AllDict = null;
    AllCsv = null;
  }

  public override void AddItem(T item, string[] props) {
    AllDict.Add(item.GetHashCode(), item);
    AllCsv.Add(new(item, props));
  }

  public void LoadProps() =>
    SimpleDB.LoadFromFile(
      line => {
        var prop = line.Split('|');
        if (prop.Length != 2)
          throw new ArgumentException("Incorrect number of values.", line);
        TableProps.Add(prop[0], prop[1]);
      }, TablePropsFilePath);

  public void SaveProps() {
    PropsToCsv();
    if (TableProps.Count == 0) return;

    if (SimpleDB.SaveToFile(TableProps.Select(x => $"{x.Key}|{x.Value}"), x => x, TablePropsFilePath))
      AreTablePropsModified = false;
  }

  public static List<TI> IdToRecord<TI>(string csv, Dictionary<int, TI> source, Func<int, TI> resolveNotFound) {
    if (string.IsNullOrEmpty(csv)) return null;

    var items = csv
      .Split(',')
      .Select(int.Parse)
      .Select(x => source.TryGetValue(x, out var rec) ? rec : resolveNotFound(x))
      .Where(x => x != null)
      .ToList();

    return items.Count == 0 ? null : items;
  }

  public List<T> LinkList(string csv, Func<int, T> getNotFoundRecord, IDataAdapter seeker) =>
    IdToRecord(csv, AllDict, notFoundId => ResolveNotFoundRecord(notFoundId, getNotFoundRecord, seeker));

  public T ResolveNotFoundRecord(int notFoundId, Func<int, T> getNotFoundRecord, IDataAdapter seeker) {
    if (getNotFoundRecord == null) return null;
    _notFoundIds ??= new();
    seeker.IsModified = true;

    if (_notFoundIds.TryGetValue(notFoundId, out var id)) return AllDict[id];

    var item = getNotFoundRecord(notFoundId);

    _notFoundIds.Add(notFoundId, item.GetHashCode());
    AllDict.Add(item.GetHashCode(), item);
    return item;
  }

  public virtual T GetById(string id, bool nullable = false) =>
    nullable && string.IsNullOrEmpty(id) ? null : AllDict[int.Parse(id)];
}