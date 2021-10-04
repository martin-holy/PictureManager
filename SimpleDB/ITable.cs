using System.Collections.Generic;

namespace SimpleDB {
  public interface ITable {
    DataAdapter DataAdapter { get; }
    List<IRecord> All { get; }
  }
}
