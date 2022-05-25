namespace SimpleDB {
  public interface IDataAdapter {
    SimpleDB DB { get; set; }
    ILogger Logger { get; set; }
    string TableName { get; }
    int MaxId { get; set; }
    bool IsModified { get; set; }
    bool AreTablePropsModified { get; set; }

    void Load();
    void Save();
    void LoadProps();
    void SaveProps();
    void LinkReferences() { }
    void Clear();
  }
}
