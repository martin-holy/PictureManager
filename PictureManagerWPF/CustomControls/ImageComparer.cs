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
    private RadioButton _rbAvgHash;

    static ImageComparer() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageComparer),
        new FrameworkPropertyMetadata(typeof(ImageComparer)));
    }

    public override void OnApplyTemplate() {
      if (Template.FindName("PART_RbAvgHash", this) is RadioButton rbAvgHash) {
        rbAvgHash.Checked += delegate { Compare(); };
        _rbAvgHash = rbAvgHash;
      }

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

    public void SelectDefaultMethod() {
      if (_rbAvgHash != null)
        _rbAvgHash.IsChecked = true;
    }

    public void Compare() {
      var items = MediaItems.Filter(App.Core.Model.MediaItems.ThumbsGrid.LoadedItems);
      List<MediaItem> similar = null;

      switch (SelectedMode) {
        case 0: {
          // Average Hash
          similar = GetSimilar(items.ToArray(), Diff, _avgHashes, GetAvgHash);
          break;
        }
        case 1: {
          // pHash
          similar = GetSimilar(items.ToArray(), Diff, _pHashes, GetPerceptualHash);
          break;
        }
      }

      App.Core.Model.MediaItems.ThumbsGrid.FilteredItems.Clear();
      if (similar != null)
        foreach (var mi in similar)
          App.Core.Model.MediaItems.ThumbsGrid.FilteredItems.Add(mi);

      App.Core.Model.MediaItems.ThumbsGrid.Current = null;
      App.Core.MediaItemsViewModel.ThumbsGridReloadItems();
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

    private static int CompareHashes(long a, long b) {
      var diff = 0;
      for (var i = 0; i < 64; i++)
        if ((a & (1 << i)) != (b & (1 << i)))
          diff++;

      return diff;
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

    private static long GetPerceptualHash(string srcPath) {
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
        var pixels = new byte[1024];
        grayScale.CopyPixels(pixels, 32, 0);
        var pixels2D = new byte[32,32];
        var row = -1;
        for (var i = 0; i < 1024; i++) {
          if (i % 32 == 0) row++;
          pixels2D[row, i - row * 32] = pixels[i];
        }

        // compute DCT
        var pixelsDct = ApplyDiscreteCosineTransform(pixels2D, 32);

        // compute average only from top-left 8x8 minus first value
        double total = 0;
        for (var x = 0; x < 8; x++) {
          for (var y = 0; y < 8; y++) {
            total += pixelsDct[x, y];
          }
        }
        total -= pixelsDct[0,0];
        var avg = total / (8 * 8 - 1);

        // compute bits
        long hash = 0;
        var bi = 0;
        for (var x = 0; x < 8; x++) {
          for (var y = 0; y < 8; y++) {
            if (pixelsDct[x,y] > avg)
              hash |= 1 << bi;
            bi++;
          }
        }

        return hash;
      }
    }

    private static double[,] ApplyDiscreteCosineTransform(byte[,] input, int size) {
      var m = size;
      var n = size;
      const double pi = 3.142857;

      // dct will store the discrete cosine transform 
      var dct = new double[m,n];

      for (var i = 0; i < m; i++) {
        for (var j = 0; j < n; j++) {
          double ci, cj;
          // ci and cj depends on frequency as well as 
          // number of row and columns of specified matrix 
          if (i == 0)
            ci = 1 / Math.Sqrt(m);
          else
            ci = Math.Sqrt(2) / Math.Sqrt(m);

          if (j == 0)
            cj = 1 / Math.Sqrt(n);
          else
            cj = Math.Sqrt(2) / Math.Sqrt(n);

          // sum will temporarily store the sum of  
          // cosine signals 
          double sum = 0;
          for (var k = 0; k < m; k++) {
            for (var l = 0; l < n; l++) {
              sum += input[k, l] *
                     Math.Cos((2 * k + 1) * i * pi / (2 * m)) *
                     Math.Cos((2 * l + 1) * j * pi / (2 * n));
            }
          }

          dct[i,j] = ci * cj * sum;
        }
      }

      return dct;
    }
  }
}
