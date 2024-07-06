using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MH.Utils.BaseClasses;

public class TableDataAdapter<T> : DataAdapter<T>, ITableDataAdapter where T : class {
  private bool _areTablePropsModified;
  private Dictionary<int, int>? _notFoundIds;

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
  public Dictionary<int, T> AllDict { get; } = [];
  public List<(T, string[])> AllCsv { get; } = [];

  public TableDataAdapter(SimpleDB db, string name, int propsCount) : base(db, name, propsCount) {
    TablePropsFilePath = Path.Combine("db", $"{name}_props.csv");
  }

  public virtual void PropsToCsv() { }
  public virtual void LinkReferences() { }

  public void Clear() {
    All = AllDict.Values.ToHashSet();
    AllDict.Clear();
    AllCsv.Clear();
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

  public static List<TI>? IdToRecord<TI>(string csv, Dictionary<int, TI> source, Func<int, TI?> resolveNotFound) {
    if (string.IsNullOrEmpty(csv)) return null;

    var items = csv
      .Split(',')
      .Select(int.Parse)
      .Select(x => source.TryGetValue(x, out var rec) ? rec : resolveNotFound(x))
      .Where(x => x != null)
      .Select(x => x!)
      .ToList();

    return items.Count == 0 ? null : items;
  }

  /// <summary>
  /// Returns List of found records and List of not found Ids
  /// </summary>
  public static Tuple<List<TI>, List<int>>? IdsToRecords<TI>(string csv, Dictionary<int, TI> source) {
    if (string.IsNullOrEmpty(csv)) return null;
    var found = new List<TI>();
    var notFound = new List<int>();

    foreach (var id in csv.Split(',').Select(int.Parse))
      if (source.TryGetValue(id, out var rec))
        found.Add(rec);
      else
        notFound.Add(id);

    return new(found, notFound);
  }

  public List<T>? LinkList(string csv, Func<int, T> getNotFoundRecord, IDataAdapter seeker) =>
    IdToRecord<T>(csv, AllDict, notFoundId => ResolveNotFoundRecord(notFoundId, getNotFoundRecord, seeker));

  public T? ResolveNotFoundRecord(int notFoundId, Func<int, T>? getNotFoundRecord, IDataAdapter seeker) {
    if (getNotFoundRecord == null) return null;
    _notFoundIds ??= new();
    seeker.IsModified = true;

    if (_notFoundIds.TryGetValue(notFoundId, out var id)) return AllDict[id];

    var item = getNotFoundRecord(notFoundId);

    _notFoundIds.Add(notFoundId, item.GetHashCode());
    AllDict.Add(item.GetHashCode(), item);
    return item;
  }

  public virtual T? GetById(string id, bool nullable = false) =>
    nullable && string.IsNullOrEmpty(id) ? null : AllDict[int.Parse(id)];
}