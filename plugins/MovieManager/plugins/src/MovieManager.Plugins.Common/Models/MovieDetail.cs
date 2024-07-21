namespace MovieManager.Plugins.Common.Models;

public class MovieDetail {
  public DetailId DetailId { get; set; }
  public string? Type { get; set; }
  public string Title { get; set; }
  public string? OriginalTitle { get; set; }
  public int Year { get; set; }
  public int YearEnd { get; set; }
  public int Runtime { get; set; }
  public string? Plot { get; set; }
  public double Rating { get; set; }
  public string[]? Genres { get; set; }
  public Image? Poster { get; set; }
  public Image[]? Images { get; set; }
  public string? MPAA { get; set; }
  public Cast[] Cast { get; set; } = [];

  public MovieDetail(DetailId detailId, string title) {
    DetailId = detailId;
    Title = title;
  }
}