using System.Collections.Generic;

namespace SimpleDB {
  public interface ITable {
    TableHelper Helper { get; set; }
    List<IRecord> All { get; }
    void NewFromCsv(string csv);
    void LinkReferences();
    void LoadFromFile();

    public void SaveToFile() => Helper.SaveToFile(All);
    public virtual void TablePropsToCsv() {}
  }
}
