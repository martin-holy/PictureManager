using MH.Utils;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace MovieManager.Plugins.Common;

public class Core {
  public static async Task DownloadAndSaveFile(string url, string filePath) {
    using var client = new HttpClient();
    var bytes = await client.GetByteArrayAsync(url).ConfigureAwait(false);
    await File.WriteAllBytesAsync(filePath, bytes);
  }

  public static async Task<string> GetWebPageContent(string url, string language = "en") {
    using var client = new HttpClient();
    client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");
    client.DefaultRequestHeaders.Add("Accept-Language", language);

    try {
      var response = await client.GetAsync(url);

      if (!response.Content.Headers.ContentEncoding.Contains("gzip"))
        return await response.Content.ReadAsStringAsync();

      var stream = await response.Content.ReadAsStreamAsync();
      return await DecompressContent(stream);

    }
    catch (Exception ex) {
      Log.Error(ex);
      return null;
    }
  }

  private static async Task<string> DecompressContent(Stream content) {
    await using var gzip = new GZipStream(content, CompressionMode.Decompress);
    using var reader = new StreamReader(gzip);
    return await reader.ReadToEndAsync();
  }
}