namespace MovieManager.Plugins.Common.Models;

public class Image(string url) {
  public string? Id { get; set; }
  public string Url { get; set; } = url;
  public int Height { get; set; }
  public int Width { get; set; }
  public string? Desc { get; set; }
}