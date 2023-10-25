using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace MH.Utils.BaseClasses; 

public class DataAdapter<T> : IDataAdapter<T> where T : class {
  private bool _isModified;
  private bool _areTablePropsModified;
  private Dictionary<int, int> _notFoundIds;
  protected string CurrentVolumeSerialNumber;

  public bool IsModified {
    get => _isModified;
    set {
      _isModified = value;
      if (value)
        DB.AddChange();
    }
  }

  public bool AreTablePropsModified {
    get => _areTablePropsModified;
    set {
      _areTablePropsModified = value;
      if (value)
        DB.AddChange();
    }
  }

  public SimpleDB DB { get; set; }
  public string TableName { get; }
  public string TableFilePath { get; }
  public string TablePropsFilePath { get; }
  public int MaxId { get; set; }
  public int PropsCount { get; }
  public Dictionary<string, string> TableProps { get; } = new();
  public Dictionary<int, T> AllDict { get; set; }
  public HashSet<T> All { get; set; }
  public List<(T, string[])> AllCsv { get; set; }

  public event EventHandler<ObjectEventArgs<T>> ItemCreatedEvent = delegate { };
  public event EventHandler<ObjectEventArgs<T>> ItemDeletedEvent = delegate { };
  public event EventHandler<ObjectEventArgs<IList<T>>> ItemsDeletedEvent = delegate { };

  public DataAdapter(string tableName, int propsCount) {
    TableName = tableName;
    PropsCount = propsCount;
    TableFilePath = Path.Combine("db", $"{tableName}.csv");
    TablePropsFilePath = Path.Combine("db", $"{tableName}_props.csv");
  }

  public virtual T FromCsv(string[] csv) => throw new NotImplementedException();
  public virtual string ToCsv(T item) => throw new NotImplementedException();
  public virtual void PropsToCsv() { }
  public virtual void LinkReferences() { }

  protected void RaiseItemCreated(T item) => ItemCreatedEvent(this, new(item));
  protected void RaiseItemDeleted(T item) => ItemDeletedEvent(this, new(item));
  protected void RaiseItemsDeleted(IList<T> items) => ItemsDeletedEvent(this, new(items));

  protected virtual void OnItemCreated(T item) { }
  protected virtual void OnItemDeleted(T item) { }
  protected virtual void OnItemsDeleted(IList<T> items) { }

  public virtual void Load() {
    AllDict = new();
    AllCsv = new();

    if (SimpleDB.LoadFromFile(ParseLine, TableFilePath)) return;

    foreach (var drive in Drives.SerialNumbers) {
      CurrentVolumeSerialNumber = drive.Value;
      SimpleDB.LoadFromFile(ParseLine, GetDBFilePath(drive.Key, TableName));
    }
  }

  public virtual void Save() =>
    SaveToFile(All);

  public void SaveToFile(IEnumerable<T> items) {
    if (SimpleDB.SaveToFile(items, ToCsv, TableFilePath))
      IsModified = false;
  }

  public void SaveDriveRelated(Dictionary<string, IEnumerable<T>> drives) {
    foreach (var (drive, items) in drives)
      SimpleDB.SaveToFile(items, ToCsv, GetDBFilePath(drive, TableName));

    // TODO should be for each drive
    IsModified = false;

    // TODO remove in future release
    if (File.Exists(TableFilePath))
      File.Delete(TableFilePath);
  }

  private static string GetDBFilePath(string drive, string tableName) {
    var oldPath = string.Join(Path.DirectorySeparatorChar, "db", $"{tableName}.{drive[..1]}.csv");
    var newPath = string.Join(Path.DirectorySeparatorChar, "db", $"{tableName}.{Drives.SerialNumbers[drive]}.csv");

    if (File.Exists(oldPath))
      File.Move(oldPath, newPath);

    return newPath;
  }

  public void Clear() {
    AllCsv = null;
    All = AllDict.Values.ToHashSet();
    AllDict = null;
  }

  public void ParseLine(string line) {
    var props = line.Split('|');
    if (props.Length != PropsCount)
      throw new ArgumentException("Incorrect number of values.", line);

    var record = FromCsv(props);

    AllDict.Add(record.GetHashCode(), record);
    AllCsv.Add(new(record, props));
  }

  public int GetNextId() {
    IsModified = true;
    return ++MaxId;
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
      .ToList();

    return items.Count == 0 ? null : items;
  }

  public List<T> LinkList(string csv, Func<int, T> getNotFoundRecord, IDataAdapter seeker) =>
    IdToRecord(csv, AllDict, notFoundId => ResolveNotFoundRecord(notFoundId, getNotFoundRecord, seeker));

  public static ObservableCollection<object> LinkObservableCollection<TI>(string csv, Dictionary<int, TI> source) {
    var records = IdToRecord(csv, source, _ => default)?.Where(x => x != null);
    if (records == null) return null;

    var collection = new ObservableCollection<object>();

    foreach (var item in records)
      collection.Add(item);

    return collection;
  }

  public T ResolveNotFoundRecord(int notFoundId, Func<int, T> getNotFoundRecord, IDataAdapter seeker) {
    _notFoundIds ??= new();
    seeker.IsModified = true;

    if (_notFoundIds.TryGetValue(notFoundId, out var id)) return AllDict[id];

    var item = getNotFoundRecord(notFoundId);

    _notFoundIds.Add(notFoundId, item.GetHashCode());
    AllDict.Add(item.GetHashCode(), item);
    return item;
  }
}