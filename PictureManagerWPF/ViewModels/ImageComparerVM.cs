using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using PictureManager.Domain.Models;
using PictureManager.Domain;
using PictureManager.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;

namespace PictureManager.ViewModels {
  public sealed class ImageComparerVM : ObservableObject {
    private readonly Dictionary<object, long> _avgHashes = new();
    private readonly Dictionary<object, long> _pHashes = new();
    private int _diff;

    public int Diff { get => _diff; set { _diff = value; OnPropertyChanged(); } }
    public delegate long HashMethod(string srcPath, Int32Rect rect);
    public RelayCommand<ThumbnailsGridM> AverageHashCommand { get; }
    public RelayCommand<ThumbnailsGridM> PHashCommand { get; }

    public ImageComparerVM() {
      AverageHashCommand = new(async (tg) => await Compare(tg, _avgHashes, Imaging.GetAvgHash));
      PHashCommand = new(async (tg) => await Compare(tg, _pHashes, Imaging.GetPerceptualHash));
    }

    public async Task Compare(ThumbnailsGridM thumbsGrid, Dictionary<object, long> hashes, HashMethod hashMethod) {
      if (thumbsGrid == null) return;

      var items = thumbsGrid.Filter(thumbsGrid.LoadedItems);
      List<object> similar = GetSimilar(items.ToArray(), Diff, hashes, hashMethod);

      thumbsGrid.GroupByFolders = false;
      thumbsGrid.GroupByDate = false;
      thumbsGrid.FilteredItems.Clear();

      if (similar != null)
        foreach (var mi in similar.Cast<MediaItemM>())
          thumbsGrid.FilteredItems.Add(mi);

      thumbsGrid.CurrentMediaItem = null;
      await thumbsGrid.ThumbsGridReloadItems();
    }

    private static List<object> GetSimilar(MediaItemM[] items, int limit, Dictionary<object, long> hashes, HashMethod hashMethod) {
      // get hashes
      if (hashes.Count == 0)
        GetHashes(items, hashes, hashMethod);

      // get similar
      return Imaging.GetSimilarImages(hashes, limit);
    }

    private static void GetHashes(MediaItemM[] items, Dictionary<object, long> hashes, HashMethod hashMethod) {
      var progress = new ProgressBarDialog("Computing Hashes ...", false, 1);
      progress.AddEvents(
        items,
        null,
        // action
        (mi) => {
          if (!hashes.ContainsKey(mi))
            hashes.Add(mi, hashMethod(mi.FilePathCache, Int32Rect.Empty));
        },
        mi => mi.FilePath,
        null);

      progress.Start();
      Core.DialogHostShow(progress);
    }
  }
}
