namespace MovieManager.Plugins.Common.Models;

public class SearchResult {
  public DetailId DetailId { get; set; }
  public string Name { get; set; }
  public int Year { get; set; }
  public string Type { get; set; }
  public string Desc { get; set; }
  public Image Image { get; set; }

  public string TypeAndYear =>
    (Type, Year) switch {
      (null or "", 0) => string.Empty,
      (_, > 0) => $"{Type} {Year}",
      (_, _) => Type,
    };
}