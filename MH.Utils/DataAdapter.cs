using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace MH.Utils {
  public abstract class DataAdapter<T> : IDataAdapter {
    private bool _isModified;
    private bool _areTablePropsModified;

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

    public DataAdapter(string tableName, int propsCount) {
      TableName = tableName;
      PropsCount = propsCount;
      TableFilePath = Path.Combine("db", $"{tableName}.csv");
      TablePropsFilePath = Path.Combine("db", $"{tableName}_props.csv");
    }

    public abstract T FromCsv(string[] csv);
    public abstract string ToCsv(T item);
    public virtual void PropsToCsv() { }
    public virtual void LinkReferences() { }

    public virtual void Load() {
      AllDict = new();
      AllCsv = new();

      if (SimpleDB.LoadFromFile(ParseLine, TableFilePath)) return;

      foreach (var drive in Environment.GetLogicalDrives())
        SimpleDB.LoadFromFile(ParseLine, GetDBFilePath(drive, TableName));
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

    private static string GetDBFilePath(string drive, string tableName) =>
      string.Join(Path.DirectorySeparatorChar, "db", $"{tableName}.{drive[..1]}.csv");

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

    public static List<TI> IdToRecord<TI>(string csv, Dictionary<int, TI> source) {
      if (string.IsNullOrEmpty(csv)) return null;

      var items = csv
        .Split(',')
        .Select(int.Parse)
        .Where(source.ContainsKey)
        .Select(x => source[x])
        .ToList();

      return items.Count == 0 ? null : items;
    }

    public static List<TI> LinkList<TI>(string csv, Dictionary<int, TI> source) =>
      IdToRecord(csv, source);

    public static ObservableCollection<object> LinkObservableCollection<TI>(string csv, Dictionary<int, TI> source) {
      var records = IdToRecord(csv, source);
      if (records == null) return null;

      var collection = new ObservableCollection<object>();

      foreach (var item in records)
        collection.Add(item);

      return collection;
    }
  }
}
