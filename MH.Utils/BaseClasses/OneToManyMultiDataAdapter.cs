using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MH.Utils.BaseClasses;

public class OneToManyMultiDataAdapter<TA, TB> : DataAdapter<KeyValuePair<TA, List<TB>>>, IRelationDataAdapter where TA : class where TB : class {
  public new Dictionary<TA, List<TB>> All { get; set; }
  public TableDataAdapter<TA> KeyDataAdapter { get; set; }
  //public IDataAdapter<TB>[] ValueDataAdapters { get; set; }

  public OneToManyMultiDataAdapter(string name, TableDataAdapter<TA> keyDa) :
    base(name, 2) {
    KeyDataAdapter = keyDa;
    //ValueDataAdapters = daB;

    KeyDataAdapter.ItemDeletedEvent += (_, e) => {
      if (All != null && All.TryGetValue(e.Data, out var b))
        ItemDelete(new(e.Data, b));
    };
  }

  public override void Load() {
    All = new();
    base.Load();
  }

  public override KeyValuePair<TA, List<TB>> FromCsv(string[] csv) =>
    new(KeyDataAdapter.GetById(csv[0]), GetByIds(csv[1]));

  public override string ToCsv(KeyValuePair<TA, List<TB>> item) =>
    string.Join("|", item.Key.GetHashCode().ToString(), item.Value.ToHashCodes().ToCsv());

  public override void AddItem(KeyValuePair<TA, List<TB>> item, string[] props) =>
    All.Add(item.Key, item.Value);

  public override KeyValuePair<TA, List<TB>> ItemCreate(KeyValuePair<TA, List<TB>> item) {
    All.Add(item.Key, item.Value);
    IsModified = true;
    RaiseItemCreated(item);
    OnItemCreated(item);
    return item;
  }

  public override void ItemDelete(KeyValuePair<TA, List<TB>> item, bool singleDelete = true) {
    All.Remove(item.Key);
    IsModified = true;
    RaiseItemDeleted(item);

    if (!singleDelete) return;
    var items = new[] { item };
    RaiseItemsDeleted(items);
    OnItemDeleted(item);
    OnItemsDeleted(items);
  }

  public virtual TB GetValueById(string id) => throw new NotImplementedException();

  public List<TB> GetByIds(string ids) =>
    ids
      .Split(',')
      .Select(GetValueById)
      .Where(x => x != null)
      .ToList();
}