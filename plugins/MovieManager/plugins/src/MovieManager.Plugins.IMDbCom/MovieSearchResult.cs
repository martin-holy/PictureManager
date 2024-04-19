using MovieManager.Plugins.Common.Interfaces;
using System.Text.Json.Serialization;

namespace MovieManager.Plugins.IMDbCom;

public class MovieSearchResult : IMovieSearchResult {
  [JsonPropertyName("l")]
  public string Name { get; set; }

  [JsonPropertyName("qid")]
  public string Type { get; set; }

  [JsonPropertyName("y")]
  public int Year { get; set; }

  [JsonPropertyName("id")]
  public string Id { get; set; }

  [JsonIgnore]
  public IDetailId DetailId { get; set; }

  [JsonPropertyName("i")]
  public Image Image { get; set; }

  IImage IMovieSearchResult.Image => Image;
}