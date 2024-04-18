using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace MovieManager.Plugins.IMDbAPIdev;

public static class Queries {
  public static JsonElement Execute(object query) {
    using var client = new HttpClient();
    var request = new HttpRequestMessage {
      RequestUri = new("https://graph.imdbapi.dev/v1"),
      Method = HttpMethod.Post,
      Content = new StringContent(JsonSerializer.Serialize(query), Encoding.UTF8, "application/json")
    };

    var response = client.SendAsync(request).GetAwaiter().GetResult();

    if (!response.IsSuccessStatusCode) {
      throw new($"GraphQL request failed with status code {response.StatusCode}");
    }

    var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    var jsonDocument = JsonDocument.Parse(responseContent);
    var responseData = jsonDocument.RootElement.GetProperty("data");

    return responseData;
  }

  public static object GetTitleById(string id, int castCount) =>
    new { query = _titleById, variables = new { id, castCount } };
  
  public static object GetNameById(string id, int avatarsCount, int knownForCount) =>
    new { query = _nameById, variables = new { id, avatarsCount, knownForCount } };

  private const string _titleById =
    """
    query titleById($id: ID!, $castCount: Int = 10) {
      title(id: $id) {
        id
        type
        primary_title
        start_year
        end_year
        runtime_minutes
        plot
        rating {
          aggregate_rating
        }
        genres
        posters {
          url
          width
          height
        }
        certificates {
          rating
        }
        casts: credits(first: $castCount, categories: ["actor", "actress"]) {
          name {
            id
            display_name
          }
          characters
        }
      }
    }
    """;

  private const string _nameById =
    """
    query nameById($id: ID!, $avatarsCount: Int = 5, $knownForCount: Int = 20) {
      name(id: $id) {
        id
        display_name
        avatars(first: $avatarsCount) {
          url
          width
          height
        }
        birth_year
        known_for(first: $knownForCount) {
          id
          primary_title
        }
      }
    }
    """;
}