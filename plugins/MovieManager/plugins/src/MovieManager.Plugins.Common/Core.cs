using MH.Utils;
using MovieManager.Plugins.Common.Interfaces;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MovieManager.Plugins.Common;

public class Core {
  public static IIMDbPlugin? IMDbPlugin { get; set; }

  public static async Task DownloadAndSaveFile(string url, string filePath) {
    using var client = new HttpClient();
    var bytes = await client.GetByteArrayAsync(url).ConfigureAwait(false);
    await File.WriteAllBytesAsync(filePath, bytes).ConfigureAwait(false);
  }

  public static async Task<string?> GetWebPageContent(string url, CancellationToken token, string language = "en") {
    using var client = new HttpClient();
    client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");
    client.DefaultRequestHeaders.Add("Accept-Language", language);

    try {
      var response = await client.GetAsync(url, token).ConfigureAwait(false);

      if (!response.Content.Headers.ContentEncoding.Contains("gzip"))
        return await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);

      var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
      return await DecompressContent(stream).ConfigureAwait(false);

    }
    catch (OperationCanceledException) {
      return null;
    }
    catch (Exception ex) {
      Log.Error(ex);
      return null;
    }
  }

  private static async Task<string> DecompressContent(Stream content) {
    await using var gzip = new GZipStream(content, CompressionMode.Decompress);
    using var reader = new StreamReader(gzip);
    return await reader.ReadToEndAsync().ConfigureAwait(false);
  }
}