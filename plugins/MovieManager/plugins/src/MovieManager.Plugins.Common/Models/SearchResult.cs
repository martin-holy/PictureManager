namespace MovieManager.Plugins.Common.Models;

public class SearchResult {
  public DetailId DetailId { get; set; }
  public string Name { get; set; }
  public int Year { get; set; }
  public string Type { get; set; }
  public string Desc { get; set; }
  public Image Image { get; set; }
}