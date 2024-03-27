using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace MovieManager.Plugins.Common;

public class Core {
  public static Task<string> GetUrlContent(string url) {
    using HttpClient client = new HttpClient();
    try {
      var response = client.GetAsync(url).Result;
      response.EnsureSuccessStatusCode();
      return response.Content.ReadAsStringAsync();
    }
    catch (HttpRequestException e) {
      Console.WriteLine($"HTTP Error: {e.Message}");
    }
    return null;
  }
}