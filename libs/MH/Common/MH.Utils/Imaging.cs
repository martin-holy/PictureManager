using System.Collections.Generic;
using System;
using System.Linq;

namespace MH.Utils;

public enum Orientation { Normal = 1, FlipHorizontal = 2, Rotate180 = 3, FlipVertical = 4, Transpose = 5, Rotate270 = 6, Transverse = 7, Rotate90 = 8 }

public static class Imaging {
  public delegate long ImageHashFunc(string srcPath);
  public delegate byte[] GetBitmapHashPixelsFunc(string filePath, int bytes);
  public delegate void ResizeJpgAction(string src, string dest, int px, bool withMetadata, bool withThumbnail, int quality);
    
  public static GetBitmapHashPixelsFunc GetBitmapHashPixels { get; set; } = null!;
  public static ResizeJpgAction ResizeJpg { get; set; } = null!;

  public static void GetThumbSize(double width, double height, int desiredSize, out int outWidth, out int outHeight) {
    // don't make the thumb bigger than image it self
    var bigger = Math.Max(width, height);
    if (desiredSize > bigger) desiredSize = (int)bigger;

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
        hash |= (uint)(1 << i);

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
          hash |= (uint)(1 << bi);
        bi++;
      }
    }

    return hash;
  }

  private static double[,] ApplyDiscreteCosineTransform(byte[,] input, int size) {
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

  public static void RgbToHsl(double r, double g, double b, out double h, out double s, out double l) {
    r /= 255.0;
    g /= 255.0;
    b /= 255.0;

    var max = Math.Max(r, Math.Max(g, b));
    var min = Math.Min(r, Math.Min(g, b));
    var delta = max - min;

    // Lightness
    l = ((max + min) / 2.0) * 240;

    if (delta == 0) {
      // Grey, no chroma
      h = 0;
      s = 0;
      return;
    }

    // Saturation
    s = (l < 120 ? delta / (max + min) : delta / (2.0 - max - min)) * 240;

    // Hue
    if (r == max) h = (g - b) / delta;
    else if (g == max) h = 2.0 + (b - r) / delta;
    else h = 4.0 + (r - g) / delta;

    h *= 40; // Convert hue to 0-239 range
    if (h < 0) h += 240;
  }

  public static void HslToRgb(double h, double s, double l, out byte r, out byte g, out byte b) {
    h = (h / 239.0) * 360;
    s /= 240.0;
    l /= 240.0;

    double dr, dg, db;

    if (s == 0) {
      dr = dg = db = l; // Achromatic
    } else {
      double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
      double p = 2 * l - q;

      dr = HueToRgb(p, q, h / 360 + 1 / 3.0);
      dg = HueToRgb(p, q, h / 360);
      db = HueToRgb(p, q, h / 360 - 1 / 3.0);
    }

    r = (byte)(dr * 255);
    g = (byte)(dg * 255);
    b = (byte)(db * 255);
  }

  public static double HueToRgb(double p, double q, double t) {
    if (t < 0) t += 1;
    if (t > 1) t -= 1;
    if (t < 1 / 6.0) return p + (q - p) * 6 * t;
    if (t < 1 / 2.0) return q;
    if (t < 2 / 3.0) return p + (q - p) * (2 / 3.0 - t) * 6;
    return p;
  }
}