using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MovieManager.Plugins.IMDbAPIdev;

public class Core {
  public void GetMovieById(string imdbId) {
    var query = $$"""
                  query titleById {
                    title(id: "{{imdbId}}") {
                      id
                      type
                      start_year
                      plot
                      genres
                      rating {
                        aggregate_rating
                        votes_count
                      }
                    }
                  }
                  """;

    ExecuteGraphQLQuery(query).ContinueWith(response => {

    });
  }

  private static async Task<string> ExecuteGraphQLQuery(string query) {
    var httpClient = new HttpClient();
    var request = new HttpRequestMessage {
      RequestUri = new("https://graph.imdbapi.dev/v1"),
      Method = HttpMethod.Post,
      Content = new StringContent(JsonSerializer.Serialize(new { query }), Encoding.UTF8, "application/json")
    };

    var httpResponse = await httpClient.SendAsync(request);

    if (!httpResponse.IsSuccessStatusCode) {
      throw new Exception($"GraphQL request failed with status code {httpResponse.StatusCode}");
    }

    var responseContent = await httpResponse.Content.ReadAsStringAsync();
    var jsonDocument = JsonDocument.Parse(responseContent);
    var responseData = jsonDocument.RootElement.GetProperty("data").ToString();

    return responseData;
  }
}