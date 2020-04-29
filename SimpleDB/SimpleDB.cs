using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SimpleDB {
  public class SimpleDB {
    public Dictionary<Type, TableHelper> Tables = new Dictionary<Type, TableHelper>();

    private readonly Dictionary<string, int> _idSequences = new Dictionary<string, int>();
    private readonly ILogger _logger;

    public SimpleDB(ILogger logger) {
      _logger = logger;
      LoadIdSequences();
    }

    public void AddTable(ITable table) {
      if (!_idSequences.TryGetValue(table.GetType().Name, out var maxId))
        _idSequences.Add(table.GetType().Name, 0);

      Tables.Add(table.GetType(), new TableHelper(table, maxId, _logger));
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
      foreach (var helper in Tables.Values) {
        if (helper.IsModified == false) continue;
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
        _logger.LogError(ex);
      }
    }

    private void SaveIdSequences() {
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
        _logger.LogError(ex);
      }
    }
  }
}
