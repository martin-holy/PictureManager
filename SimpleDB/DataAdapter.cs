using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SimpleDB {
  public abstract class DataAdapter {
    private readonly ILogger _logger;
    private readonly SimpleDB _db;

    private bool _isModified;
    private bool _areTablePropsModified;

    public bool IsModified {
      get => _isModified;
      set {
        _isModified = value;
        if (value)
          _db.AddChange();
      }
    }

    public bool AreTablePropsModified {
      get => _areTablePropsModified;
      set {
        _areTablePropsModified = value;
        if (value)
          _db.AddChange();
      }
    }

    public string TableName { get; }
    public string TableFilePath { get; }
    public string TablePropsFilePath { get; }
    public int MaxId { get; set; }
    public Dictionary<string, string> TableProps { get; } = new();

    public abstract void Load();
    public abstract void Save();
    public abstract void FromCsv(string csv);
    public virtual void PropsToCsv() { }
    public virtual void LinkReferences() { }

    public DataAdapter(string tableName, SimpleDB db) {
      TableName = tableName;
      _db = db;
      _logger = db.Logger;
      TableFilePath = Path.Combine("db", $"{tableName}.csv");
      TablePropsFilePath = Path.Combine("db", $"{tableName}_props.csv");
    }

    public int GetNextId() {
      IsModified = true;
      return ++MaxId;
    }

    public void LoadFromFile() => SimpleDB.LoadFromFile(FromCsv, TableFilePath, _logger);

    public void SaveToFile<T>(IEnumerable<T> items, Func<T, string> toCsv) {
      if (SimpleDB.SaveToFile(items, toCsv, TableFilePath, _logger))
        IsModified = false;
    }

    public void LoadProps() =>
      SimpleDB.LoadFromFile(
        (line) => {
          var prop = line.Split('|');
          if (prop.Length != 2)
            throw new ArgumentException("Incorrect number of values.", line);
          TableProps.Add(prop[0], prop[1]);
        }, TablePropsFilePath, _logger);

    public void SaveProps() {
      PropsToCsv();
      if (TableProps.Count == 0) return;

      if (SimpleDB.SaveToFile(TableProps.Select(x => $"{x.Key}|{x.Value}"), (x) => x, TablePropsFilePath, _logger))
        AreTablePropsModified = false;
    }
  }
}
