using MovieManager.Plugins.Common.Interfaces;
using System.Text.Json.Serialization;

namespace MovieManager.Plugins.MediaIMDbCom;

public class Image : IImage {
  [JsonPropertyName("height")]
  public int Height { get; set; }

  [JsonPropertyName("imageUrl")]
  public string Url { get; set; }

  [JsonPropertyName("width")]
  public int Width { get; set; }
}