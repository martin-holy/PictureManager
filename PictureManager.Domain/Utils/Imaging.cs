using PictureManager.Domain.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Storage;
using Point = System.Windows.Point;
using WGI = Windows.Graphics.Imaging;
using WMFA = Windows.Media.FaceAnalysis;

namespace PictureManager.Domain.Utils {
  public static class Imaging {
    private static readonly string[] SupportedExts = { ".jpg", ".jpeg", ".mp4" };
    private static readonly string[] SupportedImageExts = { ".jpg", ".jpeg" };

    public static bool IsSupportedFileType(string filePath) =>
      SupportedExts.Any(x => x.Equals(Path.GetExtension(filePath), StringComparison.OrdinalIgnoreCase));

    public static MediaType GetMediaType(string filePath) {
      return SupportedImageExts.Any(
        x => filePath.EndsWith(x, StringComparison.InvariantCultureIgnoreCase))
        ? MediaType.Image
        : MediaType.Video;
    }

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

    public static void ResizeJpg(string src, string dest, int px, bool withMetadata, bool withThumbnail, int quality) {
      int GreatestCommonDivisor(int a, int b) {
        while (a != 0 && b != 0) {
          if (a > b)
            a %= b;
          else
            b %= a;
        }

        return a == 0 ? b : a;
      }

      void SetIfContainsQuery(BitmapMetadata bm, string query, object value) {
        if (bm.ContainsQuery(query))
          bm.SetQuery(query, value);
      }

      var srcFile = new FileInfo(src);
      var destFile = new FileInfo(dest);

      using Stream srcFileStream = File.Open(srcFile.FullName, FileMode.Open, FileAccess.Read);
      var decoder = BitmapDecoder.Create(srcFileStream, BitmapCreateOptions.None, BitmapCacheOption.None);
      if (decoder.CodecInfo == null || !decoder.CodecInfo.FileExtensions.Contains("jpg") || decoder.Frames[0] == null) return;

      var firstFrame = decoder.Frames[0];

      var pxw = firstFrame.PixelWidth; // image width
      var pxh = firstFrame.PixelHeight; // image height
      var gcd = GreatestCommonDivisor(pxw, pxh);
      var rw = pxw / gcd; // image ratio
      var rh = pxh / gcd; // image ratio
      var q = Math.Sqrt((double)px / (rw * rh)); // Bulgarian constant
      var stw = q * rw / pxw; // scale transform X
      var sth = q * rh / pxh; // scale transform Y

      var resized = new TransformedBitmap(firstFrame, new ScaleTransform(stw, sth, 0, 0));
      var metadata = withMetadata ? firstFrame.Metadata?.Clone() as BitmapMetadata : new BitmapMetadata("jpg");
      var thumbnail = withThumbnail ? firstFrame.Thumbnail : null;

      if (!withMetadata) {
        // even when withMetadata == false, set orientation
        var orientation = ((BitmapMetadata)firstFrame.Metadata)?.GetQuery("System.Photo.Orientation") ?? (ushort)1;
        metadata.SetQuery("System.Photo.Orientation", orientation);
      }

      // ifd ImageWidth a ImageHeight
      SetIfContainsQuery(metadata, "/app1/ifd/{ushort=256}", resized.PixelWidth);
      SetIfContainsQuery(metadata, "/app1/ifd/{ushort=257}", resized.PixelHeight);
      // exif ExifImageWidth a ExifImageHeight
      SetIfContainsQuery(metadata, "/app1/ifd/exif/{ushort=40962}", resized.PixelWidth);
      SetIfContainsQuery(metadata, "/app1/ifd/exif/{ushort=40963}", resized.PixelHeight);

      var encoder = new JpegBitmapEncoder { QualityLevel = quality };

      encoder.Frames.Add(BitmapFrame.Create(resized, thumbnail, metadata, firstFrame.ColorContexts));

      using (Stream destFileStream = File.Open(destFile.FullName, FileMode.Create, FileAccess.ReadWrite)) {
        encoder.Save(destFileStream);
      }

      // set LastWriteTime to destination file as DateTaken so it can be correctly sorted in mobile apps
      var date = DateTime.MinValue;

      // try to first get dateTaken from file name
      var match = Regex.Match(srcFile.Name, "[0-9]{8}_[0-9]{6}");
      if (match.Success)
        DateTime.TryParseExact(match.Value, "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

      // try to get dateTaken from metadata
      if (date == DateTime.MinValue) {
        var dateTaken = ((BitmapMetadata)firstFrame.Metadata)?.DateTaken;
        DateTime.TryParse(dateTaken, out date);
      }

      if (date != DateTime.MinValue)
        destFile.LastWriteTime = date;
    }

    public static void ReSaveImage(string filePath, int quality) {
      // TODO: try to preserve EXIF information
      var original = new FileInfo(filePath);
      var newFile = new FileInfo(filePath + "_newFile");
      try {
        using (Stream originalFileStream = File.Open(original.FullName, FileMode.Open, FileAccess.Read)) {
          using var bmp = new Bitmap(originalFileStream);
          using Stream newFileStream = File.Open(newFile.FullName, FileMode.Create, FileAccess.ReadWrite);
          var encoder = ImageCodecInfo.GetImageDecoders().SingleOrDefault(x => x.FormatID == bmp.RawFormat.Guid);
          if (encoder == null) return;
          var encParams = new EncoderParameters(1) {
            Param = { [0] = new EncoderParameter(Encoder.Quality, quality) }
          };
          bmp.Save(newFileStream, encoder, encParams);
        }

        newFile.CreationTime = original.CreationTime;
        original.Delete();
        newFile.MoveTo(original.FullName);
      }
      catch (Exception ex) {
        if (newFile.Exists) newFile.Delete();
        Core.Instance.Logger.LogError(ex, filePath);
      }
    }

    public static Task CreateThumbnailsAsync(IReadOnlyCollection<MediaItem> items, CancellationToken token, int size, int quality, IProgress<int> progress) {
      return Task.Run(async () => {
        var count = items.Count;
        var workingOn = 0;

        await Task.WhenAll(
          from partition in Partitioner.Create(items).GetPartitions(Environment.ProcessorCount)
          select Task.Run(async delegate {
            using (partition) {
              while (partition.MoveNext()) {
                if (token.IsCancellationRequested) break;

                workingOn++;
                var workingOnInt = workingOn;

                await Core.Instance.RunOnUiThread(() => {
                  progress.Report(Convert.ToInt32((double)workingOnInt / count * 100));
                });

                var mi = partition.Current;
                // Folder can by null if the mediaItem is corrupted and is deleted in loading metadata process
                if (mi == null || mi.Folder == null) continue;
                if (File.Exists(mi.FilePathCache)) continue;
                await CreateThumbnailAsync(mi.MediaType, mi.FilePath, mi.FilePathCache, size, 0, quality);

                mi.ReloadThumbnail();
              }
            }
          }));
      }, token);
    }

    public static Task CreateThumbnailAsync(MediaType type, string srcPath, string destPath, int size, int rotationAngle, int quality) =>
      type == MediaType.Image
        ? Task.Run(() => CreateImageThumbnail(srcPath, destPath, size, quality))
        : CreateThumbnailAsync(srcPath, destPath, size, rotationAngle, quality);

    public static bool CreateImageThumbnail(string srcPath, string destPath, int desiredSize, int quality) {
      try {
        var dir = Path.GetDirectoryName(destPath);
        if (dir == null) return false;
        Directory.CreateDirectory(dir);

        using (Stream srcFileStream = File.Open(srcPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
          var decoder = BitmapDecoder.Create(srcFileStream, BitmapCreateOptions.None, BitmapCacheOption.None);
          if (decoder.CodecInfo == null || !decoder.CodecInfo.FileExtensions.Contains("jpg") ||
              decoder.Frames[0] == null) return false;

          var frame = decoder.Frames[0];
          var orientation = (MediaOrientation)((ushort?)((BitmapMetadata)frame.Metadata)?.GetQuery("System.Photo.Orientation") ?? 1);
          var rotated = orientation is MediaOrientation.Rotate90 or MediaOrientation.Rotate270;
          var pxw = (double)(rotated ? frame.PixelHeight : frame.PixelWidth);
          var pxh = (double)(rotated ? frame.PixelWidth : frame.PixelHeight);

          GetThumbSize(pxw, pxh, desiredSize, out var thumbWidth, out var thumbHeight);

          var output = new TransformedBitmap(frame, new ScaleTransform(thumbWidth / pxw, thumbHeight / pxh, 0, 0));

          if (rotated) {
            // yes, angles 90 and 270 are switched
            var angle = orientation == MediaOrientation.Rotate90 ? 270 : 90;
            output = new TransformedBitmap(output, new RotateTransform(angle));
          }

          using Stream destFileStream = File.Open(destPath, FileMode.Create, FileAccess.ReadWrite);
          var encoder = new JpegBitmapEncoder { QualityLevel = quality };
          encoder.Frames.Add(BitmapFrame.Create(output));
          encoder.Save(destFileStream);
        }

        return true;
      }
      catch (Exception) {
        return false;
      }
    }

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

    public static Task<int[]> GetImageDimensionsAsync(string filePath) {
      return Task.Run(() => {
        try {
          using Stream srcFileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
          var decoder = BitmapDecoder.Create(srcFileStream, BitmapCreateOptions.None, BitmapCacheOption.None);
          var frame = decoder.Frames[0];
          return new[] { frame.PixelWidth, frame.PixelHeight };
        }
        catch (Exception) {
          return null;
        }
      });
    }

    // Create Thumbnail from video from given time (very slow!!!)
    public static Task CreateVideoThumbnailAsync(string srcPath, string destPath, string size, string time, string ffmpegPath) {
      var args = $"-y -i \"{srcPath}\" -ss {time} -frames:v 1 -s {size} \"{destPath}\"";
      var tcs = new TaskCompletionSource<bool>();

      var process = new Process {
        EnableRaisingEvents = true,
        StartInfo = new() {
          Arguments = args,
          FileName = ffmpegPath,
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

    public static bool CreateVideoThumbnailFromVisual(FrameworkElement visual, string destPath, int desiredSize, int quality) {
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
        GetThumbSize(aw, ah, desiredSize, out var tw, out var th);
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

    /// <summary>
    /// Detect faces in an image
    /// </summary>
    /// <param name="filePath">File path to an image</param>
    /// <param name="faceBoxExpand">The extension of face box in percentage</param>
    /// <returns>The list of box coordinates around face expanded by faceBoxExpand percentage</returns>
    public static async Task<IList<Int32Rect>> DetectFaces(string filePath, int faceBoxExpand) {
      var file = await StorageFile.GetFileFromPathAsync(filePath);
      using var stream = await file.OpenAsync(FileAccessMode.Read, StorageOpenOptions.AllowOnlyReaders);
      var decoder = await WGI.BitmapDecoder.CreateAsync(stream);
      using var bitmap = await decoder.GetSoftwareBitmapAsync();

      // TODO try all BitmapPixelFormats
      using var bmp = WMFA.FaceDetector.IsBitmapPixelFormatSupported(bitmap.BitmapPixelFormat)
      ? bitmap : WGI.SoftwareBitmap.Convert(bitmap, WGI.BitmapPixelFormat.Gray8);
      var faceDetector = await WMFA.FaceDetector.CreateAsync();
      var detectedFaces = await faceDetector.DetectFacesAsync(bmp);

      // convert detected faces to List<Int32Rect> and expand rects
      return ExpandRects(detectedFaces, faceBoxExpand, bmp.PixelWidth, bmp.PixelHeight);
    }

    private static IList<Int32Rect> ExpandRects(IList<WMFA.DetectedFace> faces, int faceBoxExpand, int bmpWidth, int bmpHeight) {
      var faceBoxes = new List<Int32Rect>();

      foreach (var fBox in faces) {
        var rect = new Int32Rect(
          (int)fBox.FaceBox.X,
          (int)fBox.FaceBox.Y,
          (int)fBox.FaceBox.Width,
          (int)fBox.FaceBox.Height);

        if (faceBoxExpand == 0) {
          faceBoxes.Add(rect);
          continue;
        }

        // calc percentage expand
        var exp = (int)(fBox.FaceBox.Width / 100.0 * faceBoxExpand);
        if (exp % 2 != 0) exp++;
        var halfExp = exp / 2;

        // expand rect in a way that doesn't overflow image
        rect.X = rect.X > halfExp ? rect.X - halfExp : 0;
        rect.Y = rect.Y > halfExp ? rect.Y - halfExp : 0;
        rect.Width = rect.X + rect.Width + exp < bmpWidth ? rect.Width + exp : bmpWidth - rect.X;
        rect.Height = rect.Y + rect.Height + exp < bmpHeight ? rect.Height + exp : bmpHeight - rect.Y;

        // make it square
        if (rect.Height > rect.Width) {
          var diff = rect.Height - rect.Width;
          rect.Height -= diff;
          rect.Y += diff / 2;
        }
        else {
          var diff = rect.Width - rect.Height;
          rect.Width -= diff;
          rect.X += diff / 2;
        }

        faceBoxes.Add(rect);
      }

      return faceBoxes;
    }

    public static BitmapSource GetCroppedBitmapSource(string filePath, Int32Rect rect, int size) {
      using Stream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

      var bmp = new BitmapImage();
      bmp.BeginInit();
      bmp.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
      bmp.StreamSource = fileStream;
      bmp.SourceRect = rect;
      bmp.EndInit();
      bmp.Freeze();

      var bmpResized = bmp.Resize(size);
      bmpResized.Freeze();

      return bmpResized;
    }

    public static WriteableBitmap Resize(this BitmapSource bitmapSource, int size) {
      var pxW = bitmapSource.PixelWidth;
      var pxH = bitmapSource.PixelHeight;
      var sizeW = pxW > pxH ? size : size * (pxW / (pxH / 100.0)) / 100;
      var sizeH = pxH > pxW ? size : size * (pxH / (pxW / 100.0)) / 100;
      var scaleX = sizeW / pxW;
      var scaleY = sizeH / pxH;

      return new WriteableBitmap(new TransformedBitmap(bitmapSource, new ScaleTransform(scaleX, scaleY, 0, 0)));
    }

    public static FormatConvertedBitmap ToGray(this BitmapSource bitmapSource) =>
      new(bitmapSource, PixelFormats.Gray8, BitmapPalettes.Gray256, 0.0);

    public static CroppedBitmap Cropp(this BitmapSource bitmapSource, Int32Rect sourceRect) =>
      new(bitmapSource, sourceRect);

    public static Bitmap ToBitmap(this BitmapSource bitmapSource) {
      var format = bitmapSource.Format.ToImaging();
      var bmp = new Bitmap(bitmapSource.PixelWidth, bitmapSource.PixelHeight, format);

      if (format == System.Drawing.Imaging.PixelFormat.Format8bppIndexed) {
        var cp = bmp.Palette;
        for (var i = 0; i < 256; i++)
          cp.Entries[i] = System.Drawing.Color.FromArgb(i, i, i);
        bmp.Palette = cp;
      }

      var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);
      bitmapSource.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
      bmp.UnlockBits(data);
      return bmp;
    }

    public static System.Drawing.Imaging.PixelFormat ToImaging(this System.Windows.Media.PixelFormat pixelFormat) {
      if (pixelFormat == PixelFormats.Indexed8) return System.Drawing.Imaging.PixelFormat.Format8bppIndexed;
      if (pixelFormat == PixelFormats.Gray8) return System.Drawing.Imaging.PixelFormat.Format8bppIndexed;
      if (pixelFormat == PixelFormats.Bgr24) return System.Drawing.Imaging.PixelFormat.Format24bppRgb;
      if (pixelFormat == PixelFormats.Bgr32) return System.Drawing.Imaging.PixelFormat.Format32bppRgb;
      if (pixelFormat == PixelFormats.Bgra32) return System.Drawing.Imaging.PixelFormat.Format32bppArgb;
      throw new NotImplementedException($"Conversion from pixel format {pixelFormat} is not supported.");
    }

    public static long GetAvgHash(string filePath, Int32Rect rect) => GetAvgHashAsync(filePath, rect).Result;

    public static long GetPerceptualHash(string filePath, Int32Rect rect) => GetPerceptualHashAsync(filePath, rect).Result;

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
