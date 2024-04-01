using MovieManager.Plugins.Common.Interfaces;
using System.Text.Json.Serialization;

namespace MovieManager.Plugins.IMDbAPIdev;

public class Cast : ICast {
  [JsonPropertyName("name")]
  public CastName CastName { get; set; }

  [JsonPropertyName("characters")]
  public string[] Characters { get; set; }

  public string Id => CastName.Id;
  public string Name => CastName.Name;
}

public class CastName {
  [JsonPropertyName("id")]
  public string Id { get; set; }

  [JsonPropertyName("display_name")]
  public string Name { get; set; }
}