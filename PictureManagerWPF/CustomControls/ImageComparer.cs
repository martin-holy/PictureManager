using PictureManager.Dialogs;
using PictureManager.Domain.Models;
using PictureManager.Domain.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PictureManager.CustomControls {
  public class ImageComparer : Control {
    public static readonly DependencyProperty DiffProperty = DependencyProperty.Register(nameof(Diff), typeof(int), typeof(ImageComparer));

    public int Diff {
      get => (int)GetValue(DiffProperty);
      set => SetValue(DiffProperty, value);
    }

    public bool[] ModeArray { get; } = { true, false };
    public int SelectedMode => Array.IndexOf(ModeArray, true);

    public delegate long HashMethod(string srcPath, Int32Rect rect);

    private readonly Dictionary<object, long> _avgHashes = new();
    private readonly Dictionary<object, long> _pHashes = new();
    private RadioButton _rbAvgHash;

    static ImageComparer() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageComparer), new FrameworkPropertyMetadata(typeof(ImageComparer)));
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
      var thumbsGrid = App.Core.MediaItems.ThumbsGrid;
      var items = MediaItems.Filter(thumbsGrid.LoadedItems);
      List<object> similar = null;

      switch (SelectedMode) {
        case 0: {
          // Average Hash
          similar = GetSimilar(items.ToArray(), Diff, _avgHashes, Imaging.GetAvgHash);
          break;
        }
        case 1: {
          // pHash
          similar = GetSimilar(items.ToArray(), Diff, _pHashes, Imaging.GetPerceptualHash);
          break;
        }
      }

      thumbsGrid.GroupByFolders = false;
      thumbsGrid.GroupByDate = false;
      thumbsGrid.FilteredItems.Clear();

      if (similar != null)
        foreach (var mi in similar.Cast<MediaItem>())
          thumbsGrid.FilteredItems.Add(mi);

      thumbsGrid.Current = null;
      App.Ui.MediaItemsViewModel.ThumbsGridReloadItems();
      App.Core.MarkUsedKeywordsAndPeople();
    }

    private static List<object> GetSimilar(MediaItem[] items, int limit, Dictionary<object, long> hashes, HashMethod hashMethod) {
      // get hashes
      if (hashes.Count == 0)
        GetHashes(items, hashes, hashMethod);

      // get similar
      return Imaging.GetSimilarImages(hashes, limit);
    }

    private static void GetHashes(MediaItem[] items, Dictionary<object, long> hashes, HashMethod hashMethod) {
      var progress = new ProgressBarDialog(App.WMain, false, 1, "Computing Hashes ...");
      progress.AddEvents(
        items,
        null,
        // action
        delegate (MediaItem mi) {
          if (!hashes.ContainsKey(mi))
            hashes.Add(mi, hashMethod(mi.FilePathCache, Int32Rect.Empty));
        },
        mi => mi.FilePath,
        null);

      progress.StartDialog();
    }
  }
}
