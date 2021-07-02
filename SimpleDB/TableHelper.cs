using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SimpleDB {
  public class TableHelper {
    public int MaxId { get; set; }
    public ITable Table { get; set; }
    public bool IsModified { get; set; }
    public bool AutoLoad { get; set; }
    public bool AreTablePropsModified { get; set; }
    public Dictionary<string, string> TableProps { get; set; }
    private readonly string _tableFilePath;
    private readonly string _tablePropsFilePath;
    private readonly ILogger _logger;

    public TableHelper(ITable table, int maxId, ILogger logger, bool autoLoad) {
      _logger = logger;
      table.Helper = this;
      Table = table;
      MaxId = maxId;
      AutoLoad = autoLoad;
      _tableFilePath = Path.Combine("db", $"{Table.GetType().Name}.csv");
      _tablePropsFilePath = Path.Combine("db", $"{Table.GetType().Name}_props.csv");
    }

    public int GetNextId() {
      IsModified = true;
      return ++MaxId;
    }

    public void LoadFromFile() {
      if (!File.Exists(_tableFilePath)) return;
      try {
        using var sr = new StreamReader(_tableFilePath, Encoding.UTF8);
        string line;
        while ((line = sr.ReadLine()) != null)
          Table.NewFromCsv(line);
      }
      catch (Exception ex) {
        _logger.LogError(ex);
      }
    }

    public void SaveToFile(IEnumerable<IRecord> records) {
      try {
        using var sw = new StreamWriter(_tableFilePath, false, Encoding.UTF8, 65536);
        foreach (var item in records)
          sw.WriteLine(item.ToCsv());

        IsModified = false;
      }
      catch (Exception ex) {
        _logger.LogError(ex);
      }
    }

    public void LoadPropsFromFile() {
      if (!File.Exists(_tablePropsFilePath)) return;
      try {
        TableProps = new();
        using var sr = new StreamReader(_tablePropsFilePath, Encoding.UTF8);
        string line;
        while ((line = sr.ReadLine()) != null) {
          var prop = line.Split('|');
          if (prop.Length != 2) throw new ArgumentException("Incorrect number of values.", line);
          TableProps.Add(prop[0], prop[1]);
        }
      }
      catch (Exception ex) {
        _logger.LogError(ex);
      }
    }

    public void SaveTablePropsToFile() {
      Table.TablePropsToCsv();
      if (TableProps == null) return;
      try {
        using var sw = new StreamWriter(_tablePropsFilePath, false, Encoding.UTF8, 65536);
        foreach (var prop in TableProps)
          sw.WriteLine($"{prop.Key}|{prop.Value}");

        AreTablePropsModified = false;
      }
      catch (Exception ex) {
        _logger.LogError(ex);
      }
    }
  }
}