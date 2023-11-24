using System.Collections.Generic;
using System;
using System.Linq;

namespace MH.Utils;

public static class Imaging {
  public delegate long ImageHashFunc(string srcPath);
  public delegate byte[] GetBitmapHashPixelsFunc(string filePath, int bytes);
  public delegate void ResizeJpgAction(string src, string dest, int px, bool withMetadata, bool withThumbnail, int quality);
    
  public static GetBitmapHashPixelsFunc GetBitmapHashPixels { get; set; }
  public static ResizeJpgAction ResizeJpg { get; set; }

  public static void GetThumbSize(double width, double height, int desiredSize, out int outWidth, out int outHeight) {
    if (width > height) {
      //panorama
      if (width / height > 16.0 / 9.0) {
        const int maxWidth = 1100;
        var panoramaHeight = desiredSize / 16.0 * 9;
        var tooBig = panoramaHeight / height * width > maxWidth;
        outHeight = (int)(tooBig ? maxWidth / width * height : panoramaHeight);
        outWidth = (int)(tooBig ? maxWidth : panoramaHeight / height * width);
        if (outHeight % 2 != 0) outHeight++;
        if (outWidth % 2 != 0) outWidth++;
        return;
      }

      outHeight = (int)(desiredSize / width * height);
      outWidth = desiredSize;
      if (outHeight % 2 != 0) outHeight++;
      return;
    }

    outHeight = desiredSize;
    outWidth = (int)(desiredSize / height * width);
    if (outWidth % 2 != 0) outWidth++;
  }

  public static long GetBitmapAvgHash(string filePath) =>
    GetBitmapAvgHash(GetBitmapHashPixels(filePath, 8));

  public static long GetBitmapAvgHash(byte[] pixels) {
    // compute average
    var sum = 0;
    for (var i = 0; i < 64; i++)
      sum += pixels[i];
    var avg = sum / 64;

    // compute bits
    long hash = 0;
    for (var i = 0; i < 64; i++)
      if (pixels[i] > avg)
        hash |= 1 << i;

    return hash;
  }

  public static long GetBitmapPerceptualHash(string filePath) =>
    GetBitmapPerceptualHash(GetBitmapHashPixels(filePath, 32));

  public static long GetBitmapPerceptualHash(byte[] pixels) {
    // copy pixels
    var pixels2D = new byte[32, 32];
    var row = -1;
    for (var i = 0; i < 1024; i++) {
      if (i % 32 == 0) row++;
      pixels2D[row, i - (row * 32)] = pixels[i];
    }

    // compute DCT
    var pixelsDct = ApplyDiscreteCosineTransform(pixels2D, 32);

    // compute average only from top-left 8x8 minus first value
    double total = 0;
    for (var x = 0; x < 8; x++)
    for (var y = 0; y < 8; y++)
      total += pixelsDct[x, y];

    total -= pixelsDct[0, 0];
    var avg = total / ((8 * 8) - 1);

    // compute bits
    long hash = 0;
    var bi = 0;
    for (var x = 0; x < 8; x++) {
      for (var y = 0; y < 8; y++) {
        if (pixelsDct[x, y] > avg)
          hash |= 1 << bi;
        bi++;
      }
    }

    return hash;
  }

  public static double[,] ApplyDiscreteCosineTransform(byte[,] input, int size) {
    var m = size;
    var n = size;
    const double pi = 3.142857;

    // dct will store the discrete cosine transform 
    var dct = new double[m, n];

    for (var i = 0; i < m; i++) {
      for (var j = 0; j < n; j++) {
        // ci and cj depends on frequency as well as 
        // number of row and columns of specified matrix 
        var ci = i == 0 ? 1 / Math.Sqrt(m) : Math.Sqrt(2) / Math.Sqrt(m);
        var cj = j == 0 ? 1 / Math.Sqrt(n) : Math.Sqrt(2) / Math.Sqrt(n);

        // sum will temporarily store the sum of  
        // cosine signals 
        double sum = 0;
        for (var k = 0; k < m; k++) {
          for (var l = 0; l < n; l++) {
            sum += input[k, l] *
                   Math.Cos(((2 * k) + 1) * i * pi / (2 * m)) *
                   Math.Cos(((2 * l) + 1) * j * pi / (2 * n));
          }
        }

        dct[i, j] = ci * cj * sum;
      }
    }

    return dct;
  }

  /// <summary>
  /// Gets list of images ordered by similarity
  /// </summary>
  /// <param name="hashes">Image object and hash dictionary</param>
  /// <param name="limit">Similarity output limit. Set -1 to no limit</param>
  /// <returns>List of images ordered by similarity</returns>
  public static List<object> GetSimilarImages(Dictionary<object, long> hashes, int limit) {
    var items = hashes.Keys.ToArray();
    var itemsLength = items.Length;
    var output = new List<object>();
    var set = new HashSet<int>();

    if (itemsLength == 1) {
      output.Add(items[0]);
      return output;
    }

    for (var i = 0; i < itemsLength; i++) {
      var similar = new Dictionary<int, int>();

      for (var j = i + 1; j < itemsLength; j++) {
        var diff = CompareHashes(hashes[items[i]], hashes[items[j]]);
        if (diff > limit) continue;
        similar.Add(j, diff);
      }

      if (similar.Count == 0) continue;

      // add similar
      if (set.Add(i)) output.Add(items[i]);
      foreach (var s in similar.OrderBy(x => x.Value))
        if (set.Add(s.Key))
          output.Add(items[s.Key]);
    }

    return output;
  }

  public static int CompareHashes(long a, long b) {
    var diff = 0;
    for (var i = 0; i < 64; i++)
      if ((a & (1 << i)) != (b & (1 << i)))
        diff++;

    return diff;
  }
}