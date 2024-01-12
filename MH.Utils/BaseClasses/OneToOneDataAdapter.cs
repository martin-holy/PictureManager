using MH.Utils.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace MH.Utils.BaseClasses;

public class OneToOneDataAdapter<TA, TB> : DataAdapter<KeyValuePair<TA, TB>>, IRelationDataAdapter where TA : class where TB : class {
  public TableDataAdapter<TA> DataAdapterA { get; }
  public TableDataAdapter<TB> DataAdapterB { get; }
  public new Dictionary<TA, TB> All { get; set; }

  public OneToOneDataAdapter(string name, TableDataAdapter<TA> daA, TableDataAdapter<TB> daB) :
    base(name, 2) {
    DataAdapterA = daA;
    DataAdapterB = daB;

    DataAdapterA.ItemDeletedEvent += (_, e) => {
      if (All != null && All.TryGetValue(e.Data, out var b))
        ItemDelete(new(e.Data, b));
    };

    DataAdapterB.ItemDeletedEvent += (_, e) => {
      if (All != null && All.SingleOrDefault(x => ReferenceEquals(x.Value, e.Data)) is var ab)
        ItemDelete(ab);
    };
  }

  public override void Load() {
    All = new();
    base.Load();
  }

  public override KeyValuePair<TA, TB> FromCsv(string[] csv) =>
    new(DataAdapterA.GetById(csv[0]), DataAdapterB.GetById(csv[1]));

  public override string ToCsv(KeyValuePair<TA, TB> item) =>
    string.Join("|", item.Key.GetHashCode().ToString(), item.Value.GetHashCode().ToString());

  public override void AddItem(KeyValuePair<TA, TB> item, string[] props) =>
    All.Add(item.Key, item.Value);

  public override KeyValuePair<TA, TB> ItemCreate(KeyValuePair<TA, TB> item) {
    All.Add(item.Key, item.Value);
    IsModified = true;
    RaiseItemCreated(item);
    OnItemCreated(item);
    return item;
  }

  public override void ItemDelete(KeyValuePair<TA, TB> item, bool singleDelete = true) {
    All.Remove(item.Key);
    IsModified = true;
    RaiseItemDeleted(item);
    
    if (!singleDelete) return;
    var items = new[] { item };
    RaiseItemsDeleted(items);
    OnItemDeleted(item);
    OnItemsDeleted(items);
  }

  public virtual void ItemUpdate(KeyValuePair<TA, TB> item) {
    if (item.Key == null) return;
    
    if (All.TryGetValue(item.Key, out var oldValue)) {
      if (item.Value == null) {
        ItemDelete(new(item.Key, oldValue));
        return;
      }

      if (ReferenceEquals(item.Value, oldValue)) return;
      ItemDelete(new(item.Key, oldValue));
    }

    if (item.Value == null) return;

    ItemCreate(new(item.Key, item.Value));
  }

  public TB GetBy(TA a) =>
    a == null || !All.TryGetValue(a, out var b) ? default : b;
}