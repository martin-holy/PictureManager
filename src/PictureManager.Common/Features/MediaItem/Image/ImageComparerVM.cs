using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.MediaItem.Image;

public sealed class ImageComparerVM(MediaItemM[] items) : ObservableObject {
  private readonly Dictionary<object, long> _avgHashes = new();
  private readonly Dictionary<object, long> _pHashes = new();
  private int _diff;

  public int Diff { get => _diff; set { _diff = value; OnPropertyChanged(); } }

  public async Task<List<MediaItemM>> CompareAverageHash() =>
    (await _getSimilar(items, Diff, _avgHashes, Imaging.GetBitmapAvgHash)).Cast<MediaItemM>().ToList();

  public async Task<List<MediaItemM>> ComparePHash() =>
    (await _getSimilar(items, Diff, _pHashes, Imaging.GetBitmapPerceptualHash)).Cast<MediaItemM>().ToList();

  private static async Task<List<object>> _getSimilar(MediaItemM[] items, int limit, Dictionary<object, long> hashes, Imaging.ImageHashFunc hashMethod) {
    // get hashes
    var newItems = items.Where(x => !hashes.ContainsKey(x)).ToArray();
    if (newItems.Length > 0)
      await Dialog.ShowAsync(new ComputeImageHashesDialog(items, hashes, hashMethod));

    // get similar
    var toCompare = hashes.Where(x => items.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
    return Imaging.GetSimilarImages(toCompare, limit);
  }
}