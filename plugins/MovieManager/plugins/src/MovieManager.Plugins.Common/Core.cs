using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MovieManager.Plugins.Common;

public class Core {
  public static Task<string> GetUrlContent(string url) {
    using var client = new HttpClient();
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

  public static string DownloadAndSaveFile(string url, string filePath) {
    using var client = new HttpClient();
    var bytes = client.GetByteArrayAsync(url).Result;
    File.WriteAllBytes(filePath, bytes);
    return filePath;
  }
}