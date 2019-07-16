using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PictureManager.Database {
  public class SimpleDB {
    public Dictionary<Type, TableHelper> Tables = new Dictionary<Type, TableHelper>();

    public TableHelper Table<T>() {
      return Tables[typeof(T)];
    }

    public void AddTable(ITable table) {
      Tables.Add(table.GetType(), new TableHelper(table));
    }

    public void LoadAllTables() {
      foreach (var table in Tables) {
        App.SplashScreen.AddMessage($"Loading data for {table.Key.Name}");
        table.Value.Load();
      }
    }

    public void LinkReferences() {
      foreach (var table in Tables) {
        App.SplashScreen.AddMessage($"Linking references for {table.Key.Name}");
        table.Value.Table.LinkReferences(this);
      }
    }

    public void SaveAllTables() {
      Directory.CreateDirectory("db");
      foreach (var table in Tables)
        table.Value.Save();
      SaveIdSequences();
    }

    public void SaveIdSequences() {
      try {
        using (var sw = new StreamWriter(Path.Combine("db", "IdSequences.csv"), false, Encoding.UTF8)) {
          foreach (var table in Tables)
            sw.WriteLine(string.Join("|", table.Key.Name, table.Value.MaxId));
        }
      }
      catch (Exception ex) {
        // ignored
      }
    }
  }

  public class TableHelper {
    public int MaxId { get; set; }
    public ITable Table { get; set; }
    private readonly string _tableFilePath;

    public TableHelper(ITable table) {
      table.Helper = this;
      Table = table;
      MaxId = GetMaxId();
      _tableFilePath = Path.Combine("db", $"{Table.GetType().Name}.csv");
    }

    private int GetMaxId() {
      var maxId = 0;
      var filePath = Path.Combine("db", "IdSequences.csv");
      try {
        if (!File.Exists(filePath)) return maxId;
        using (var sr = new StreamReader(filePath, Encoding.UTF8)) {
          string line;
          var tableName = Table.GetType().Name;
          while ((line = sr.ReadLine()) != null) {
            var vals = line.Split('|');
            if (vals.Length != 2) continue;
            if (!vals[0].Equals(tableName)) continue;
            maxId = int.Parse(vals[1]);
          }
        }
      }
      catch (Exception ex) {
        // ignored
      }

      return maxId;
    }

    public int GetNextId() {
      return ++MaxId;
    }

    public void AddRecord(IRecord record) {
      Table.Records.Add(record.Id, record);
    }

    public void DeleteRecord(IRecord record) {
      Table.Records.Remove(record.Id);
    }

    public void Load() {
      Table.Records.Clear();
      if (!File.Exists(_tableFilePath)) return;
      try {
        using (var sr = new StreamReader(_tableFilePath, Encoding.UTF8)) {
          string line;
          while ((line = sr.ReadLine()) != null)
            Table.NewFromCsv(line);
        }
      }
      catch (Exception ex) {
        // ignored
      }
    }

    public void Save() {
      try {
        using (var sw = new StreamWriter(_tableFilePath, false, Encoding.UTF8, 65536)) {
          foreach (var item in Table.Records)
            sw.WriteLine(item.Value.ToCsv());
        }
      }
      catch (Exception ex) {
        // ignored
      }
    }
  }

  public interface ITable {
    TableHelper Helper { get; set; }
    Dictionary<int, IRecord> Records { get; set; }
    void NewFromCsv(string csv);
    void LinkReferences(SimpleDB sdb);
  }

  public interface IRecord {
    int Id { get; set; }
    string[] Csv { get; set; }
    string ToCsv();
  }
}
