using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace MH.Utils {
  public class SimpleDB : ObservableObject {
    private readonly List<IDataAdapter> _dataAdapters = new();
    private readonly Dictionary<string, int> _idSequences = new();
    private readonly string _isSequencesFilePath;
    private int _changes;
    private bool _needBackUp;

    public int Changes { get => _changes; set { _changes = value; OnPropertyChanged(); } }

    public SimpleDB() {
      Directory.CreateDirectory("db");
      _isSequencesFilePath = Path.Combine("db", "IdSequences.csv");
      LoadIdSequences();
    }

    public void AddDataAdapter(IDataAdapter dataAdapter) {
      if (!_idSequences.TryGetValue(dataAdapter.TableName, out var maxId))
        _idSequences.Add(dataAdapter.TableName, 0);

      dataAdapter.DB = this;
      dataAdapter.MaxId = maxId;
      _dataAdapters.Add(dataAdapter);
    }

    public void ClearDataAdapters() {
      foreach (var da in _dataAdapters)
        da.Clear();
    }

    public void LoadAllTables(IProgress<string> progress) {
      foreach (var da in _dataAdapters) {
        progress?.Report($"Loading data for {da.TableName}");
        da.Load();
        da.LoadProps();
      }
    }

    public void LinkReferences(IProgress<string> progress) {
      foreach (var da in _dataAdapters) {
        progress?.Report($"Loading data for {da.TableName}");
        try {
          da.LinkReferences();
        }
        catch (Exception ex) {
          Log.Error(ex, da.TableName);
        }
      }
    }

    public void SaveAllTables() {
      foreach (var da in _dataAdapters.Where(x => x.IsModified))
        da.Save();

      foreach (var da in _dataAdapters.Where(x => x.AreTablePropsModified))
        da.SaveProps();

      SaveIdSequences();
      Changes = 0;
    }

    private void LoadIdSequences() {
      LoadFromFile(
        line => {
          var vals = line.Split('|');
          if (vals.Length != 2)
            throw new ArgumentException("Incorrect number of values.", line);
          _idSequences.Add(vals[0], int.Parse(vals[1]));
        },
        _isSequencesFilePath);
    }

    public void SaveIdSequences() {
      // check if something changed
      var isModified = false;
      foreach (var da in _dataAdapters) {
        if (_idSequences[da.TableName] == da.MaxId) continue;
        _idSequences[da.TableName] = da.MaxId;
        isModified = true;
      }

      if (!isModified) return;

      SaveToFile(
        _dataAdapters,
        x => string.Join("|", x.TableName, x.MaxId),
        _isSequencesFilePath);
    }

    public void AddChange() {
      Changes++;
      _needBackUp = true;
    }

    public void BackUp() {
      if (!_needBackUp) return;

      try {
        using var zip = ZipFile.Open(Path.Combine("db", DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".zip"), ZipArchiveMode.Create);
        var schemaFilePath = Path.Combine("db", "SchemaVersion");
        zip.CreateEntryFromFile(schemaFilePath, schemaFilePath);

        foreach (var file in Directory.EnumerateFiles("db", "*.csv"))
          _ = zip.CreateEntryFromFile(file, file);
      }
      catch (Exception ex) {
        Log.Error(ex, "Error while backing up database.");
      }
    }

    public static bool LoadFromFile(Action<string> parseLine, string filePath) {
      if (!File.Exists(filePath)) return false;
      try {
        using var sr = new StreamReader(filePath, Encoding.UTF8);
        string line;
        while ((line = sr.ReadLine()) != null)
          parseLine(line);

        return true;
      }
      catch (Exception ex) {
        Log.Error(ex);
        return false;
      }
    }

    public static bool SaveToFile<T>(IEnumerable<T> items, Func<T, string> toString, string filePath) {
      try {
        using var sw = new StreamWriter(filePath, false, Encoding.UTF8, 65536);
        foreach (var item in items)
          sw.WriteLine(toString(item));

        return true;
      }
      catch (Exception ex) {
        Log.Error(ex);
        return false;
      }
    }

    public static void Migrate(int newVersion, Action<int, int> migrationResolver) {
      try {
        var oldVersion = 0;
        var vFilePath = Path.Combine("db", "SchemaVersion");

        if (File.Exists(vFilePath))
          oldVersion = int.Parse(File.ReadAllLines(vFilePath, Encoding.UTF8)[0]);

        if (oldVersion != newVersion) {
          migrationResolver(oldVersion, newVersion);
          File.WriteAllText(vFilePath, newVersion.ToString(), Encoding.UTF8);
        }
      }
      catch (Exception ex) {
        Log.Error(ex);
      }
    }

    public static void MigrateFile(string filePath, Func<string, string> migrateRecord) {
      if (!File.Exists(filePath)) return;

      var newFilePath = filePath + "_tmpFile";
      string line;
      using var sr = new StreamReader(filePath, Encoding.UTF8);
      using var sw = new StreamWriter(newFilePath, false, Encoding.UTF8, 65536);

      while ((line = sr.ReadLine()) != null)
        sw.WriteLine(migrateRecord(line));

      sr.Close();
      sw.Close();
      File.Move(newFilePath, filePath, true);
    }
  }
}
