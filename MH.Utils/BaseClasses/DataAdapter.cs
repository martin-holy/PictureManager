using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace MH.Utils.BaseClasses;

public class DataAdapter : IDataAdapter {
  private bool _isModified;
  protected string CurrentVolumeSerialNumber;

  public SimpleDB DB { get; set; }
  public string Name { get; }
  public string FilePath { get; }
  public int PropsCount { get; }
  public int MaxId { get; set; }
  public bool IsDriveRelated { get; set; }

  public bool IsModified {
    get => _isModified;
    set {
      _isModified = value;
      if (value)
        DB.AddChange();
    }
  }

  public DataAdapter(string name, int propsCount) {
    Name = name;
    PropsCount = propsCount;
    FilePath = Path.Combine("db", $"{name}.csv");
  }

  public virtual void Load() => throw new NotImplementedException();
  public virtual void Save() => throw new NotImplementedException();

  public double? ToDouble(string s) =>
    string.IsNullOrEmpty(s) ? null : double.Parse(s, CultureInfo.InvariantCulture);

  public string ToString(double? d) =>
    d == null ? string.Empty : ((double)d).ToString(CultureInfo.InvariantCulture);

  public virtual int GetNextId() => ++MaxId;
}

public class DataAdapter<T> : DataAdapter {
  public HashSet<T> All { get; set; }

  public event EventHandler<ObjectEventArgs<T>> ItemCreatedEvent = delegate { };
  public event EventHandler<ObjectEventArgs<T>> ItemUpdatedEvent = delegate { };
  public event EventHandler<ObjectEventArgs<T>> ItemDeletedEvent = delegate { };
  public event EventHandler<ObjectEventArgs<IList<T>>> ItemsDeletedEvent = delegate { };

  public DataAdapter(string name, int propsCount) : base(name, propsCount) { }

  protected void RaiseItemCreated(T item) => ItemCreatedEvent(this, new(item));
  protected void RaiseItemUpdated(T item) => ItemUpdatedEvent(this, new(item));
  protected void RaiseItemDeleted(T item) => ItemDeletedEvent(this, new(item));
  protected void RaiseItemsDeleted(IList<T> items) => ItemsDeletedEvent(this, new(items));

  protected virtual void OnItemCreated(T item) { }
  protected virtual void OnItemUpdated(T item) { }
  protected virtual void OnItemDeleted(T item) { }
  protected virtual void OnItemsDeleted(IList<T> items) { }

  public virtual T FromCsv(string[] csv) => throw new NotImplementedException();
  public virtual string ToCsv(T item) => throw new NotImplementedException();
  public virtual void AddItem(T item, string[] props) => throw new NotImplementedException();
  public virtual Dictionary<string, IEnumerable<T>> GetAsDriveRelated() => throw new NotImplementedException();

  public override void Load() {
    if (IsDriveRelated)
      LoadDriveRelated();
    else
      LoadFromSingleFile();
  }

  public void LoadDriveRelated() {
    foreach (var drive in Drives.SerialNumbers) {
      CurrentVolumeSerialNumber = drive.Value;
      SimpleDB.LoadFromFile(ParseLine, SimpleDB.GetDBFilePath(drive.Key, Name));
    }
  }

  public void LoadFromSingleFile() =>
    SimpleDB.LoadFromFile(ParseLine, FilePath);

  public override void Save() {
    if (IsDriveRelated)
      SaveDriveRelated(GetAsDriveRelated());
    else
      SaveToSingleFile(All);
  }

  public void SaveDriveRelated(Dictionary<string, IEnumerable<T>> drives) {
    foreach (var (drive, items) in drives)
      SimpleDB.SaveToFile(items, ToCsv, SimpleDB.GetDBFilePath(drive, Name));

    // TODO should be for each drive
    IsModified = false;

    // TODO remove in future release
    if (File.Exists(FilePath))
      File.Delete(FilePath);
  }

  public void SaveToSingleFile(IEnumerable<T> items) {
    if (SimpleDB.SaveToFile(items, ToCsv, FilePath))
      IsModified = false;
  }

  public virtual void ParseLine(string line) {
    var props = line.Split('|');
    if (props.Length != PropsCount)
      throw new ArgumentException("Incorrect number of values.", line);

    AddItem(FromCsv(props), props);
  }

  public virtual void Modify(T item) {
    IsModified = true;
  }

  public virtual T ItemCreate(T item) {
    All.Add(item);
    IsModified = true;
    RaiseItemCreated(item);
    OnItemCreated(item);
    return item;
  }

  public virtual void ItemDelete(T item, bool singleDelete = true) {
    if (singleDelete) {
      ItemsDelete(new[] { item });
      return;
    }

    All.Remove(item);
    IsModified = true;
    RaiseItemDeleted(item);
  }

  public virtual void ItemsDelete(IList<T> items) {
    if (items == null || items.Count == 0) return;
    foreach (var item in items) ItemDelete(item, false);
    RaiseItemsDeleted(items);
    OnItemsDeleted(items);
    foreach (var item in items) OnItemDeleted(item);
  }
}