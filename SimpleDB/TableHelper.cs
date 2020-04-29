using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SimpleDB {
  public class TableHelper {
    public int MaxId { get; set; }
    public ITable Table { get; set; }
    public bool IsModified { get; set; }
    private readonly string _tableFilePath;
    private readonly ILogger _logger;

    public TableHelper(ITable table, int maxId, ILogger logger) {
      _logger = logger;
      table.Helper = this;
      Table = table;
      MaxId = maxId;
      _tableFilePath = Path.Combine("db", $"{Table.GetType().Name}.csv");
    }

    public int GetNextId() {
      IsModified = true;
      return ++MaxId;
    }

    public void LoadFromFile() {
      if (!File.Exists(_tableFilePath)) return;
      try {
        using (var sr = new StreamReader(_tableFilePath, Encoding.UTF8)) {
          string line;
          while ((line = sr.ReadLine()) != null)
            Table.NewFromCsv(line);
        }
      }
      catch (Exception ex) {
        _logger.LogError(ex);
      }
    }

    public void SaveToFile(IEnumerable<IRecord> records) {
      try {
        using (var sw = new StreamWriter(_tableFilePath, false, Encoding.UTF8, 65536)) {
          foreach (var item in records)
            sw.WriteLine(item.ToCsv());
        }
      }
      catch (Exception ex) {
        _logger.LogError(ex);
      }
    }
  }
}