namespace SimpleDB {
  public interface IRecord {
    int Id { get; }
    string[] Csv { get; set; }
    string ToCsv();
  }
}
