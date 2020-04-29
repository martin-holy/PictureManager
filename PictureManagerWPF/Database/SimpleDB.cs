using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PictureManager.Database {
  public class SimpleDb {
    public Dictionary<Type, TableHelper> Tables = new Dictionary<Type, TableHelper>();

    private readonly Dictionary<string, int> _idSequences = new Dictionary<string, int>();

    public SimpleDb() {
      LoadIdSequences();
    }

    public void AddTable(ITable table) {
      if (!_idSequences.TryGetValue(table.GetType().Name, out var maxId))
        _idSequences.Add(table.GetType().Name, 0);

      Tables.Add(table.GetType(), new TableHelper(table, maxId));
    }

    public void LoadAllTables(IProgress<string> progress) {
      foreach (var table in Tables) {
        progress.Report($"Loading data for {table.Key.Name}");
        table.Value.Table.LoadFromFile();
      }
    }

    public void LinkReferences(IProgress<string> progress) {
      foreach (var table in Tables) {
        progress.Report($"Loading data for {table.Key.Name}");
        table.Value.Table.LinkReferences();
      }
    }

    public void SaveAllTables() {
      Directory.CreateDirectory("db");
      foreach (var helper in Tables.Values.Where(x => x.IsModified)) {
        helper.Table.SaveToFile();
        helper.IsModified = false;
      }

      SaveIdSequences();
    }

    private void LoadIdSequences() {
      var filePath = Path.Combine("db", "IdSequences.csv");
      try {
        if (!File.Exists(filePath)) return;
        using (var sr = new StreamReader(filePath, Encoding.UTF8)) {
          string line;
          while ((line = sr.ReadLine()) != null) {
            var vals = line.Split('|');
            if (vals.Length != 2) continue;
            _idSequences.Add(vals[0], int.Parse(vals[1]));
          }
        }
      }
      catch (Exception ex) {
        App.Core.LogError(ex);
      }
    }

    public void SaveIdSequences() {
      // check if something changed
      var isModified = false;
      foreach (var table in Tables) {
        if (_idSequences[table.Key.Name] == table.Value.MaxId) continue;
        _idSequences[table.Key.Name] = table.Value.MaxId;
        isModified = true;
      }

      if (!isModified) return;

      try {
        using (var sw = new StreamWriter(Path.Combine("db", "IdSequences.csv"), false, Encoding.UTF8)) {
          foreach (var table in Tables)
            sw.WriteLine(string.Join("|", table.Key.Name, table.Value.MaxId));
        }
      }
      catch (Exception ex) {
        App.Core.LogError(ex);
      }
    }
  }

  public class TableHelper {
    public int MaxId { get; set; }
    public ITable Table { get; set; }
    public bool IsModified { get; set; }
    private readonly string _tableFilePath;

    public TableHelper(ITable table, int maxId) {
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
        App.Core.LogError(ex);
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
        App.Core.LogError(ex);
      }
    }
  }

  public interface ITable {
    TableHelper Helper { get; set; }
    void NewFromCsv(string csv);
    void LinkReferences();
    void SaveToFile();
    void LoadFromFile();
  }

  public interface IRecord {
    int Id { get; }
    string[] Csv { get; set; }
    string ToCsv();
  }
}
