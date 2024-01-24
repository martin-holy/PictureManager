using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace MH.Utils;

public class SimpleDB : ObservableObject {
  private readonly List<ITableDataAdapter> _tableDAs = new();
  private readonly List<IRelationDataAdapter> _relationDAs = new();
  private readonly string _isSequencesFilePath;
  private int _changes;
  private bool _needBackUp;

  public int Changes { get => _changes; set { _changes = value; OnPropertyChanged(); } }
  public Dictionary<string, int> IdSequences { get; } = new();
  public bool IsReady { get; private set; }

  public event EventHandler ReadyEvent = delegate { };

  public SimpleDB() {
    Directory.CreateDirectory("db");
    _isSequencesFilePath = Path.Combine("db", "IdSequences.csv");
    LoadIdSequences();
  }

  public void SetIsReady() {
    IsReady = true;
    RaiseReadyEvent();
  }

  private void RaiseReadyEvent() => ReadyEvent(this, EventArgs.Empty);

  public void AddTableDataAdapter(ITableDataAdapter dataAdapter) {
    if (!IdSequences.TryGetValue(dataAdapter.Name, out var maxId))
      IdSequences.Add(dataAdapter.Name, 0);

    dataAdapter.DB = this;
    dataAdapter.MaxId = maxId;
    _tableDAs.Add(dataAdapter);
  }

  public void AddRelationDataAdapter(IRelationDataAdapter rda) {
    rda.DB = this;
    _relationDAs.Add(rda);
  }

  public void ClearDataAdapters() {
    foreach (var da in _tableDAs)
      da.Clear();
  }

  public void LoadAllTables(IProgress<string> progress) {
    foreach (var da in _tableDAs) {
      progress?.Report($"Loading data for {da.Name}");
      da.Load();
      da.LoadProps();
    }

    foreach (var rda in _relationDAs) {
      progress?.Report($"Loading data for {rda.Name}");
      rda.Load();
    }
  }

  public void LinkReferences(IProgress<string> progress) {
    foreach (var da in _tableDAs) {
      progress?.Report($"Loading data for {da.Name}");
      try {
        da.LinkReferences();
      }
      catch (Exception ex) {
        Log.Error(ex, da.Name);
      }
    }
  }

  public void SaveAllTables() {
    foreach (var da in _tableDAs.Where(x => x.IsModified))
      da.Save();

    foreach (var da in _tableDAs.Where(x => x.AreTablePropsModified))
      da.SaveProps();

    foreach (var rda in _relationDAs.Where(x => x.IsModified))
      rda.Save();

    SaveIdSequences();
    Changes = 0;
  }

  private void LoadIdSequences() {
    LoadFromFile(
      line => {
        var vals = line.Split('|');
        if (vals.Length != 2)
          throw new ArgumentException("Incorrect number of values.", line);
        IdSequences.Add(vals[0], int.Parse(vals[1]));
      },
      _isSequencesFilePath);
  }

  public void SaveIdSequences() {
    // check if something changed
    var isModified = false;
    foreach (var da in _tableDAs) {
      if (IdSequences[da.Name] == da.MaxId) continue;
      IdSequences[da.Name] = da.MaxId;
      isModified = true;
    }

    if (!isModified) return;

    SaveToFile(
      _tableDAs,
      x => string.Join("|", x.Name, x.MaxId),
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
      while (sr.ReadLine() is { } line)
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

  public static int? GetNextRecycledId(HashSet<int> usedIds) {
    if (!usedIds.Any()) return null;

    var id = 0;
    var max = usedIds.Max();

    for (var i = 1; i < max + 1; i++)
      if (!usedIds.Contains(i)) {
        id = i;
        break;
      }

    if (id == 0) return null;
    return id;
  }

  public static string GetDBFilePath(string drive, string tableName) {
    var oldPath = string.Join(Path.DirectorySeparatorChar, "db", $"{tableName}.{drive[..1]}.csv");
    var newPath = string.Join(Path.DirectorySeparatorChar, "db", $"{tableName}.{Drives.SerialNumbers[drive]}.csv");

    if (File.Exists(oldPath))
      File.Move(oldPath, newPath);

    return newPath;
  }
}