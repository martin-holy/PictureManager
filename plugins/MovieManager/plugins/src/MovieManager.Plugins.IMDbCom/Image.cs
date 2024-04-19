using MovieManager.Plugins.Common.Interfaces;
using System.Text.Json.Serialization;

namespace MovieManager.Plugins.IMDbCom;

public class Image : IImage {
  private string _url;

  [JsonPropertyName("id")]
  public string Id { get; set; }

  [JsonPropertyName("height")]
  public int Height { get; set; }

  [JsonPropertyName("imageUrl")]
  public string ImageUrl { get; set; }

  public string Url {
    get => string.IsNullOrEmpty(_url) ? ImageUrl : _url;
    set => _url = value;
  }

  [JsonPropertyName("width")]
  public int Width { get; set; }

  public string Desc { get; set; }
}