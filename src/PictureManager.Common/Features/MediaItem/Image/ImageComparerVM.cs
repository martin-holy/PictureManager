﻿using MH.UI.Controls;
using MH.UI.Dialogs;
using MH.Utils;
using MH.Utils.BaseClasses;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Common.Features.MediaItem.Image;

public sealed class ImageComparerVM : ObservableObject {
  private readonly Dictionary<object, long> _avgHashes = new();
  private readonly Dictionary<object, long> _pHashes = new();
  private int _diff;

  public int Diff { get => _diff; set { _diff = value; OnPropertyChanged(); } }
  public static RelayCommand<MediaItemsViewVM> AverageHashCommand { get; set; } = null!;
  public static RelayCommand<MediaItemsViewVM> PHashCommand { get; set; } = null!;

  public ImageComparerVM() {
    AverageHashCommand = new(x => Compare(x, _avgHashes, Imaging.GetBitmapAvgHash), Res.IconCompare, "Compare images using average hash");
    PHashCommand = new(x => Compare(x, _pHashes, Imaging.GetBitmapPerceptualHash), Res.IconCompare, "Compare images using perceptual hash");
  }

  private void Compare(MediaItemsViewVM? view, Dictionary<object, long> hashes, Imaging.ImageHashFunc hashMethod) {
    if (view == null) return;

    var items = view.LoadedItems.Where(x => view.Filter.Filter(x));
    var similar = GetSimilar(items.ToArray(), Diff, hashes, hashMethod);

    view.FilteredItems.Clear();
    view.Selected.DeselectAll();

    if (similar.Count > 0)
      view.FilteredItems.AddRange(similar.Cast<MediaItemM>());

    view.SoftLoad(view.FilteredItems, false, false);
  }

  private static List<object> GetSimilar(MediaItemM[] items, int limit, Dictionary<object, long> hashes, Imaging.ImageHashFunc hashMethod) {
    // get hashes
    var newItems = items.Where(x => !hashes.ContainsKey(x)).ToArray();
    if (newItems.Length > 0)
      GetHashes(newItems, hashes, hashMethod);

    // get similar
    var toCompare = hashes.Where(x => items.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
    return Imaging.GetSimilarImages(toCompare, limit);
  }

  private static void GetHashes(MediaItemM[] items, Dictionary<object, long> hashes, Imaging.ImageHashFunc hashMethod) {
    var progress = new ProgressBarAsyncDialog("Computing Hashes ...", Res.IconCompare, true, 1);
    progress.Init(
      items,
      null,
      // action
      mi => {
        if (!hashes.ContainsKey(mi))
          hashes.Add(mi, hashMethod(mi.FilePathCache));
      },
      mi => mi.FilePath,
      null);

    progress.Start();
    Dialog.Show(progress);
  }
}