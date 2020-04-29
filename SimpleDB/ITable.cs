namespace SimpleDB {
  public interface ITable {
    TableHelper Helper { get; set; }
    void NewFromCsv(string csv);
    void LinkReferences();
    void SaveToFile();
    void LoadFromFile();
  }
}
