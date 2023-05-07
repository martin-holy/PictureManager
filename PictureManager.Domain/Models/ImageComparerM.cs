using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using PictureManager.Domain.Utils;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Models {
  public sealed class ImageComparerM : ObservableObject {
    private readonly Dictionary<object, long> _avgHashes = new();
    private readonly Dictionary<object, long> _pHashes = new();
    private int _diff;

    public int Diff { get => _diff; set { _diff = value; OnPropertyChanged(); } }
    public RelayCommand<ThumbnailsGridM> AverageHashCommand { get; }
    public RelayCommand<ThumbnailsGridM> PHashCommand { get; }

    public ImageComparerM() {
      AverageHashCommand = new((tg) => Compare(tg, _avgHashes, Imaging.GetAvgHash));
      PHashCommand = new((tg) => Compare(tg, _pHashes, Imaging.GetPerceptualHash));
    }

    public void Compare(ThumbnailsGridM thumbsGrid, Dictionary<object, long> hashes, Imaging.ImageHashFunc hashMethod) {
      if (thumbsGrid == null) return;

      var items = thumbsGrid.LoadedItems.Where(x => thumbsGrid.Filter.Filter(x));
      List<object> similar = GetSimilar(items.ToArray(), Diff, hashes, hashMethod);

      thumbsGrid.GroupByFolders = false;
      thumbsGrid.GroupByDate = false;
      thumbsGrid.FilteredItems.Clear();
      thumbsGrid.DeselectAll();

      if (similar != null)
        thumbsGrid.FilteredItems.AddRange(similar.Cast<MediaItemM>());

      thumbsGrid.SoftLoad(thumbsGrid.FilteredItems, false, false);
    }

    private static List<object> GetSimilar(MediaItemM[] items, int limit, Dictionary<object, long> hashes, Imaging.ImageHashFunc hashMethod) {
      // get hashes
      if (hashes.Count == 0)
        GetHashes(items, hashes, hashMethod);

      // get similar
      return Imaging.GetSimilarImages(hashes, limit);
    }

    private static void GetHashes(MediaItemM[] items, Dictionary<object, long> hashes, Imaging.ImageHashFunc hashMethod) {
      var progress = new ProgressBarDialog("Computing Hashes ...", Res.IconCompare, true, 1);
      progress.AddEvents(
        items,
        null,
        // action
        (mi) => {
          if (!hashes.ContainsKey(mi))
            hashes.Add(mi, hashMethod(mi.FilePathCache));
        },
        mi => mi.FilePath,
        null);

      progress.Start();
      Core.DialogHostShow(progress);
    }
  }
}
