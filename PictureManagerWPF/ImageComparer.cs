using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PictureManager.Database;

namespace PictureManager {
  public static class ImageComparer {
    public static long GetHash(string srcPath) {
      using (Stream srcFileStream = File.Open(srcPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
        // get bitmap frame
        var decoder = BitmapDecoder.Create(srcFileStream, BitmapCreateOptions.None, BitmapCacheOption.None);
        if (decoder.Frames[0] == null) return 0;
        var frame = decoder.Frames[0];

        // resize
        var scaled = new TransformedBitmap(frame, new ScaleTransform(8.0 / frame.PixelWidth, 8.0 / frame.PixelHeight));

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
      }
    }

    public static int Compare(long a, long b) {
      var diff = 0;
      for (var i = 0; i < 64; i++)
        if ((a & (1 << i)) != (b & (1 << i)))
          diff++;

      return diff;
    }

    public static List<MediaItem> GetSimilar(MediaItem[] items, int limit) {
      var itemsLength = items.Length;
      var output = new List<MediaItem>();

      // get hashes
      var hashes = new Dictionary<int, long>();
      for (var i = 0; i < itemsLength; i++)
        hashes.Add(i, GetHash(items[i].FilePathCache));

      // find similar
      var set = new HashSet<int>();
      for (var i = 0; i < itemsLength; i++) {
        var similar = new Dictionary<int, int>();

        for (var j = i + 1; j < itemsLength; j++) {
          var diff = Compare(hashes[i], hashes[j]);
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
  }
}