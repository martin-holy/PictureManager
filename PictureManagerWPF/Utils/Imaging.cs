using PictureManager.Domain;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace PictureManager.Utils {
  public static class Imaging {
    public static Rotation MediaOrientation2Rotation(MediaOrientation mo) =>
      mo switch {
        MediaOrientation.Rotate90 => Rotation.Rotate270,
        MediaOrientation.Rotate180 => Rotation.Rotate180,
        MediaOrientation.Rotate270 => Rotation.Rotate90,
        _ => Rotation.Rotate0
      };

    public static BitmapImage GetBitmapImage(string filePath, MediaOrientation rotation) {
      if (filePath == null)
        return null;

      var src = new BitmapImage();
      src.BeginInit();
      src.UriSource = new(filePath);
      src.CacheOption = BitmapCacheOption.OnLoad;
      src.CreateOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;
      src.Rotation = MediaOrientation2Rotation(rotation);
      src.EndInit();

      return src;
    }

    public static Task CreateThumbnailAsync(MediaType type, string srcPath, string destPath, int size, int rotationAngle, int quality) =>
      type == MediaType.Image
        ? Task.Run(() => CreateImageThumbnail(srcPath, destPath, size, quality))
        : CreateThumbnailAsync(srcPath, destPath, size, rotationAngle, quality);

    public static Task CreateThumbnailAsync(string srcPath, string destPath, int size, int rotationAngle, int quality) {
      var tcs = new TaskCompletionSource<bool>();
      var process = new Process {
        EnableRaisingEvents = true,
        StartInfo = new() {
          Arguments = $"src|\"{srcPath}\" dest|\"{destPath}\" quality|\"{quality}\" size|\"{size}\" rotationAngle|\"{rotationAngle}\"",
          FileName = "ThumbnailCreator.exe",
          UseShellExecute = false,
          CreateNoWindow = true
        }
      };

      process.Exited += (s, e) => {
        tcs.TrySetResult(true);
        process.Dispose();
      };

      process.Start();
      return tcs.Task;
    }

    public static void CreateImageThumbnail(string srcPath, string destPath, int desiredSize, int quality) {
      var dir = Path.GetDirectoryName(destPath);
      if (dir == null) throw new ArgumentException($"Invalid destination path. {destPath}");
      Directory.CreateDirectory(dir);

      using (Stream srcFileStream = File.Open(srcPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
        var decoder = BitmapDecoder.Create(srcFileStream, BitmapCreateOptions.None, BitmapCacheOption.None);
        if (decoder.CodecInfo?.FileExtensions.Contains("jpg") != true || decoder.Frames[0] == null)
          throw new BadImageFormatException($"Image is not a JPG. {srcPath}");

        var frame = decoder.Frames[0];
        var orientation = (MediaOrientation)((ushort?)((BitmapMetadata)frame.Metadata)?.GetQuery("System.Photo.Orientation") ?? 1);
        var rotated = orientation is MediaOrientation.Rotate90 or MediaOrientation.Rotate270;
        var pxw = (double)(rotated ? frame.PixelHeight : frame.PixelWidth);
        var pxh = (double)(rotated ? frame.PixelWidth : frame.PixelHeight);

        Domain.Utils.Imaging.GetThumbSize(pxw, pxh, desiredSize, out var thumbWidth, out var thumbHeight);

        var output = new TransformedBitmap(frame, new ScaleTransform(thumbWidth / pxw, thumbHeight / pxh, 0, 0));
        var metadata = new BitmapMetadata("jpg");
        metadata.SetQuery("System.Photo.Orientation", (ushort)orientation);

        using Stream destFileStream = File.Open(destPath, FileMode.Create, FileAccess.ReadWrite);
        var encoder = new JpegBitmapEncoder { QualityLevel = quality };
        encoder.Frames.Add(BitmapFrame.Create(output, null, metadata, frame.ColorContexts));
        encoder.Save(destFileStream);
      }
    }

    public static void CreateVideoClipThumbnail(string destPath) =>
      CreateThumbnailFromVisual(
        AppCore.FullVideo,
        destPath,
        Core.Settings.ThumbnailSize,
        Core.Settings.JpegQualityLevel);

    public static bool CreateThumbnailFromVisual(FrameworkElement visual, string destPath, int desiredSize, int quality) {
      try {
        // create destination directory if doesn't exist
        var dir = Path.GetDirectoryName(destPath);
        if (dir == null) return false;
        Directory.CreateDirectory(dir);

        // get offset of visual from its parent
        var offset = visual.TranslatePoint(new Point(0, 0), (UIElement)visual.Parent);

        // round all variables for safer calculation
        var ox = Math.Round(offset.X, 0);
        var oy = Math.Round(offset.Y, 0);
        var aw = Math.Round(visual.ActualWidth, 0);
        var ah = Math.Round(visual.ActualHeight, 0);

        // render visual to bitmap
        var bmp = new RenderTargetBitmap((int)(aw + ox), (int)(ah + oy), 96, 96, PixelFormats.Pbgra32);
        bmp.Render(visual);

        // crop bitmap
        var crop = new CroppedBitmap(bmp, new Int32Rect((int)ox, (int)oy, (int)aw, (int)ah));

        // scale bitmap to thumbnail
        Domain.Utils.Imaging.GetThumbSize(aw, ah, desiredSize, out var tw, out var th);
        var thumb = new TransformedBitmap(crop, new ScaleTransform(tw / aw, th / ah, 0, 0));

        // create encoder for thumbnail
        var encoder = new JpegBitmapEncoder { QualityLevel = quality };
        encoder.Frames.Add(BitmapFrame.Create(thumb));

        // save thumbnail
        using Stream destFileStream = File.Open(destPath, FileMode.Create, FileAccess.ReadWrite);
        encoder.Save(destFileStream);

        return true;
      }
      catch (Exception) {
        return false;
      }
    }

    public static long GetAvgHash(string filePath) =>
      GetAvgHashAsync(filePath, Int32Rect.Empty).GetAwaiter().GetResult();

    public static long GetPerceptualHash(string filePath) =>
      GetPerceptualHashAsync(filePath, Int32Rect.Empty).GetAwaiter().GetResult();

    /// <summary>
    /// Compute AVG hash from image
    /// </summary>
    /// <param name="filePath">File path to the image</param>
    /// <param name="rect">Compute AVG hash only on a part of the image. Use Int32Rect.Empty for the whole image.</param>
    /// <returns>AVG hash of the image</returns>
    public static Task<long> GetAvgHashAsync(string filePath, Int32Rect rect) {
      return Task.Run(() => {
        // create source
        using Stream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var bmpSource = new BitmapImage();
        bmpSource.BeginInit();
        bmpSource.CacheOption = BitmapCacheOption.None;
        bmpSource.StreamSource = fileStream;
        bmpSource.SourceRect = rect;
        bmpSource.EndInit();

        // resize
        var scaled = new TransformedBitmap(bmpSource, new ScaleTransform(8.0 / bmpSource.PixelWidth, 8.0 / bmpSource.PixelHeight));

        // convert to gray scale
        var grayScale = new FormatConvertedBitmap(scaled, PixelFormats.Gray8, BitmapPalettes.Gray256, 0.0);

        // copy pixels
        var pixels = new byte[64];
        grayScale.CopyPixels(pixels, 8, 0);

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
      });
    }

    /// <summary>
    /// Compute Perceptual hash from image
    /// </summary>
    /// <param name="filePath">File path to the image</param>
    /// <param name="rect">Compute Perceptual hash only on a part of the image. Use Int32Rect.Empty for the whole image.</param>
    /// <returns>Perceptual hash of the image</returns>
    public static Task<long> GetPerceptualHashAsync(string filePath, Int32Rect rect) {
      return Task.Run(() => {
        // create source
        using Stream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var bmpSource = new BitmapImage();
        bmpSource.BeginInit();
        bmpSource.CacheOption = BitmapCacheOption.None;
        bmpSource.StreamSource = fileStream;
        bmpSource.SourceRect = rect;
        bmpSource.EndInit();

        // resize
        var scaled = new TransformedBitmap(bmpSource, new ScaleTransform(32.0 / bmpSource.PixelWidth, 32.0 / bmpSource.PixelHeight));

        // convert to gray scale
        var grayScale = new FormatConvertedBitmap(scaled, PixelFormats.Gray8, BitmapPalettes.Gray256, 0.0);

        // copy pixels
        var pixels = new byte[1024];
        grayScale.CopyPixels(pixels, 32, 0);
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
      });
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
}
