using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PictureManager.Dialogs;
using PictureManager.Domain.Models;

namespace PictureManager.CustomControls {
  public class ImageComparer : Control {
    public static readonly DependencyProperty DiffProperty = DependencyProperty.Register(
      nameof(Diff), typeof(int), typeof(ImageComparer));

    public int Diff {
      get => (int) GetValue(DiffProperty);
      set => SetValue(DiffProperty, value);
    }

    public bool[] ModeArray { get; } = {true, false};
    public int SelectedMode => Array.IndexOf(ModeArray, true);

    public delegate long HashMethod(string srcPath);

    private readonly Dictionary<MediaItem, long> _avgHashes = new Dictionary<MediaItem, long>();
    private readonly Dictionary<MediaItem, long> _pHashes = new Dictionary<MediaItem, long>();

    static ImageComparer() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageComparer),
        new FrameworkPropertyMetadata(typeof(ImageComparer)));
    }

    public override void OnApplyTemplate() {
      if (Template.FindName("PART_RbAvgHash", this) is RadioButton rbAvgHash)
        rbAvgHash.Checked += delegate { Compare(); };

      if (Template.FindName("PART_RbPHash", this) is RadioButton rbpHash)
        rbpHash.Checked += delegate { Compare(); };

      if (Template.FindName("PART_SliderDiff", this) is Slider sliderDiff)
        sliderDiff.ValueChanged += delegate { Compare(); };

      if (Template.FindName("PART_BtnClose", this) is Button btnClose)
        btnClose.Click += delegate { Close(); };

      base.OnApplyTemplate();
    }

    public void Close() {
      _avgHashes.Clear();
      _pHashes.Clear();
      Visibility = Visibility.Collapsed;
    }

    public void Compare() {
      var items = MediaItems.Filter(App.Core.Model.MediaItems.LoadedItems);
      List<MediaItem> similar = null;

      switch (SelectedMode) {
        case 0: {
          // Average Hash
          similar = GetSimilar(items.ToArray(), Diff, _avgHashes, GetAvgHash);
          break;
        }
        case 1: {
          // pHash
          similar = GetSimilar(items.ToArray(), Diff, _pHashes, GetPHash);
          break;
        }
      }

      App.Core.Model.MediaItems.FilteredItems.Clear();
      if (similar != null)
        foreach (var mi in similar)
          App.Core.Model.MediaItems.FilteredItems.Add(mi);

      App.Core.MediaItemsViewModel.SplittedItemsReload();
      App.Core.Model.MediaItems.Current = null;
      App.Core.Model.MarkUsedKeywordsAndPeople();
    }

    private static List<MediaItem> GetSimilar(MediaItem[] items, int limit, Dictionary<MediaItem, long> hashes, HashMethod hashMethod) {
      var itemsLength = items.Length;
      var output = new List<MediaItem>();

      // get hashes
      if (hashes.Count == 0)
        GetHashes(items, hashes, hashMethod);

      // find similar
      var set = new HashSet<int>();
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

    private static void GetHashes(MediaItem[] items, Dictionary<MediaItem, long> hashes, HashMethod hashMethod) {
      var progress = new ProgressBarDialog(App.WMain, false, 1, "Computing Hashes ...");
      progress.AddEvents(
        items,
        null,
        // action
        delegate (MediaItem mi) {
          if (!hashes.ContainsKey(mi))
            hashes.Add(mi, hashMethod(mi.FilePathCache));
        },
        mi => mi.FilePath,
        null);

      progress.StartDialog();
    }

    private static long GetAvgHash(string srcPath) {
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

    public static long GetPHash(string srcPath) {
      using (Stream srcFileStream = File.Open(srcPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
        // get bitmap frame
        var decoder = BitmapDecoder.Create(srcFileStream, BitmapCreateOptions.None, BitmapCacheOption.None);
        if (decoder.Frames[0] == null) return 0;
        var frame = decoder.Frames[0];

        // resize
        var scaled = new TransformedBitmap(frame, new ScaleTransform(32.0 / frame.PixelWidth, 32.0 / frame.PixelHeight));

        // convert to gray scale
        var grayScale = new FormatConvertedBitmap(scaled, PixelFormats.Gray8, BitmapPalettes.Gray256, 0.0);

        // copy pixels
        var pixels = new double[1024];
        grayScale.CopyPixels(pixels, 32, 0);

        // compute DCT
        Transform(pixels, 0, pixels.Length, new double[pixels.Length]);

        // get only top-left 8x8
        var lowFreq = new double[64];
        var lfi = 0;
        for (var pi = 0; pi < 256; pi++) {
          lowFreq[lfi] = pixels[pi];
          lfi++;
          if (lfi % 8 == 0) pi += 24;

        }

        // compute average
        var sum = 0.0;
        for (var i = 0; i < 64; i++)
          sum += lowFreq[i];
        var avg = sum / 64;

        // compute bits
        long hash = 0;
        for (var i = 0; i < 64; i++)
          if (lowFreq[i] > avg)
            hash |= 1 << i;

        return hash;
      }
    }

    private static void Transform(double[] vector, int off, int len, double[] temp) {
      // Algorithm by Byeong Gi Lee, 1984. For details, see:
      // See: http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.118.3056&rep=rep1&type=pdf#page=34
      if (len == 1)
        return;
      int halfLen = len / 2;
      for (int i = 0; i < halfLen; i++) {
        double x = vector[off + i];
        double y = vector[off + len - 1 - i];
        temp[off + i] = x + y;
        temp[off + i + halfLen] = (x - y) / (Math.Cos((i + 0.5) * Math.PI / len) * 2);
      }
      Transform(temp, off, halfLen, vector);
      Transform(temp, off + halfLen, halfLen, vector);
      for (int i = 0; i < halfLen - 1; i++) {
        vector[off + i * 2 + 0] = temp[off + i];
        vector[off + i * 2 + 1] = temp[off + i + halfLen] + temp[off + i + halfLen + 1];
      }
      vector[off + len - 2] = temp[off + halfLen - 1];
      vector[off + len - 1] = temp[off + len - 1];
    }

    private static int CompareHashes(long a, long b) {
      var diff = 0;
      for (var i = 0; i < 64; i++)
        if ((a & (1 << i)) != (b & (1 << i)))
          diff++;

      return diff;
    }
  }
}
