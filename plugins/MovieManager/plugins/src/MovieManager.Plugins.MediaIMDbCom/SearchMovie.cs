using MovieManager.Plugins.Common.Interfaces;
using System.Text.Json.Serialization;

namespace MovieManager.Plugins.MediaIMDbCom;

public class Movie : ISearchMovie {
  [JsonPropertyName("i")]
  public Image Image { get; set; }

  [JsonPropertyName("id")]
  public string Id { get; set; }

  [JsonPropertyName("l")]
  public string Name { get; set; }

  [JsonPropertyName("qid")]
  public string Type { get; set; }

  [JsonPropertyName("y")]
  public int Year { get; set; }

  IImage ISearchMovie.Image => Image;
}