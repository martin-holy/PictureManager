using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace SimpleDB {
  public class SimpleDB : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public Dictionary<Type, TableHelper> Tables = new Dictionary<Type, TableHelper>();
    public int Changes { get => _changes; set { _changes = value; OnPropertyChanged(); } }

    private readonly Dictionary<string, int> _idSequences = new Dictionary<string, int>();
    private readonly ILogger _logger;
    private int _changes;

    public SimpleDB(ILogger logger) {
      _logger = logger;
      Directory.CreateDirectory("db");
      LoadIdSequences();
    }

    public void AddTable(ITable table, bool autoLoad = true) {
      if (!_idSequences.TryGetValue(table.GetType().Name, out var maxId))
        _idSequences.Add(table.GetType().Name, 0);

      Tables.Add(table.GetType(), new TableHelper(table, maxId, _logger, autoLoad));
    }

    public void LoadAllTables(IProgress<string> progress) {
      foreach (var table in Tables) {
        if (!table.Value.AutoLoad) continue;
        progress.Report($"Loading data for {table.Key.Name}");
        table.Value.Table.LoadFromFile();
      }
    }

    public void LinkReferences(IProgress<string> progress) {
      foreach (var table in Tables) {
        if (!table.Value.AutoLoad) continue;
        progress.Report($"Loading data for {table.Key.Name}");
        try {
          table.Value.Table.LinkReferences();
        }
        catch (Exception ex) {
          _logger.LogError(ex, table.Key.Name);
        }
      }
    }

    public void SaveAllTables() {
      foreach (var helper in Tables.Values.Where(x => x.IsModified))
        helper.Table.SaveToFile();

      SaveIdSequences();
      Changes = 0;
    }

    private void LoadIdSequences() {
      var filePath = Path.Combine("db", "IdSequences.csv");
      try {
        if (!File.Exists(filePath)) return;
        using var sr = new StreamReader(filePath, Encoding.UTF8);
        string line;
        while ((line = sr.ReadLine()) != null) {
          var vals = line.Split('|');
          if (vals.Length != 2) throw new ArgumentException("Incorrect number of values.", line);
          _idSequences.Add(vals[0], int.Parse(vals[1]));
        }
      }
      catch (Exception ex) {
        _logger.LogError(ex);
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
        using var sw = new StreamWriter(Path.Combine("db", "IdSequences.csv"), false, Encoding.UTF8);
        foreach (var table in Tables)
          sw.WriteLine(string.Join("|", table.Key.Name, table.Value.MaxId));
      }
      catch (Exception ex) {
        _logger.LogError(ex);
      }
    }

    public void SetModified<T>() {
      if (!Tables.ContainsKey(typeof(T))) return;
      Tables[typeof(T)].IsModified = true;
      Changes++;
    }
  }
}
