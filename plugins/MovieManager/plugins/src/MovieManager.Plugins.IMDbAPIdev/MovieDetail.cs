using MovieManager.Plugins.Common.Interfaces;
using System.Text.Json.Serialization;

namespace MovieManager.Plugins.IMDbAPIdev;

public class MovieDetail : IMovieDetail {
  [JsonPropertyName("id")]
  public string Id { get; set; }

  [JsonPropertyName("type")]
  public string Type { get; set; }

  [JsonPropertyName("primary_title")]
  public string Title { get; set; }

  [JsonPropertyName("start_year")]
  public int Year { get; set; }

  [JsonPropertyName("end_year")]
  public int? YearEnd { get; set; }

  [JsonPropertyName("runtime_minutes")]
  public int Length { get; set; }

  [JsonPropertyName("plot")]
  public string Plot { get; set; }

  [JsonIgnore]
  public double Rating { get; set; }

  [JsonPropertyName("genres")]
  public string[] Genres { get; set; }

  [JsonPropertyName("posters")]
  public Image[] Posters { get; set; }

  [JsonPropertyName("casts")]
  public Cast[] Casts { get; set; }

  public IDetailId DetailId { get; set; }
  public string[] MPAA { get; set; }
  IImage[] IMovieDetail.Posters => Posters;
  ICast[] IMovieDetail.Casts => Casts;
}