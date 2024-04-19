using System.Text.Json.Serialization;
using MovieManager.Plugins.Common.Interfaces;

namespace MovieManager.Plugins.IMDbCom;

public class MovieDetail {
  public string Id { get; set; }
  public string Type { get; set; }
  public string Title { get; set; }
  public string OriginalTitle { get; set; }
  public int Year { get; set; }
  public int? YearEnd { get; set; }
  public int Runtime { get; set; }
  public string Plot { get; set; }
  public double Rating { get; set; }
  public string[] Genres { get; set; }
  public Image Poster { get; set; }
  public string Certificate { get; set; }
  //public Cast[] Casts { get; set; }
}