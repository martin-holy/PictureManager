using MH.Utils.Interfaces;
using System.Collections.Generic;

namespace MH.Utils.BaseClasses;

public class OneToOneDataAdapter<TA, TB> : DataAdapter<KeyValuePair<TA, TB>>, IRelationDataAdapter where TA : class where TB : class {
  public TableDataAdapter<TA> DataAdapterA { get; }
  public TableDataAdapter<TB> DataAdapterB { get; }

  public OneToOneDataAdapter(string name, TableDataAdapter<TA> daA, TableDataAdapter<TB> daB) :
    base(name, 2) {
    DataAdapterA = daA;
    DataAdapterB = daB;
  }

  public override KeyValuePair<TA, TB> FromCsv(string[] csv) =>
    new(DataAdapterA.GetById(csv[0]), DataAdapterB.GetById(csv[1]));

  public override string ToCsv(KeyValuePair<TA, TB> item) =>
    string.Join("|", item.Key.GetHashCode().ToString(), item.Value.GetHashCode().ToString());
}