using MH.UI.WPF.Controls;
using MH.UI.WPF.Utils;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.EventsArgs;
using MH.Utils.HelperClasses;
using PictureManager.Converters;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PictureManager.ViewModels {
  public sealed class SegmentsVM : ObservableObject {
    private readonly Core _core;
    private readonly AppCore _coreVM;
    private readonly IProgress<int> _progress;
    private readonly Dictionary<SegmentM, System.Drawing.Bitmap> _compareBitmaps = new();
    
    private readonly WorkTask _workTask = new();

    public SegmentsM SegmentsM { get; }
    public SegmentsRectsVM SegmentsRectsVM { get; }

    public RelayCommand<MouseButtonEventArgs> SelectCommand { get; }
    public RelayCommand<object> CompareCommand { get; }

    public SegmentsVM(Core core, AppCore coreVM, SegmentsM segmentsM) {
      _core = core;
      _coreVM = coreVM;
      SegmentsM = segmentsM;
      
      _progress = new Progress<int>(x => {
        _core.TitleProgressBarM.ValueA = x;
        _core.TitleProgressBarM.ValueB = x;
      });

      SegmentsM.MainTabsItem = new(this, "Segment Matching");
      SegmentsRectsVM = new(SegmentsM.SegmentsRectsM);

      SelectCommand = new(Select);
      CompareCommand = new(async () => {
        await CompareAsync();
        SegmentsM.Reload(true, true);
      });
    }

    private void Select(MouseButtonEventArgs e) {
      if (e.OriginalSource is Image { DataContext: SegmentM segmentM } image) {
        var rowStackPanel = image.TryFindParent<StackPanel>();
        var wrapPanel = image.TryFindParent<VirtualizingWrapPanel>();
        var rowIndex = wrapPanel.WrappedItems.IndexOf(rowStackPanel.DataContext);
        var itemsGroup = wrapPanel.WrappedItems
          .OfType<ItemsGroup>()
          .LastOrDefault(x => wrapPanel.WrappedItems.IndexOf(x) < rowIndex);
        var list = itemsGroup != null
          ? itemsGroup.Items
          : wrapPanel.WrappedItems
             .OfType<VirtualizingWrapPanelRow>()
             .Where(x => wrapPanel.WrappedItems.IndexOf(x) <= rowIndex)
             .SelectMany(x => x.Items);

        var people = list.OfType<PersonM>().ToList();
        var segments = list.OfType<SegmentM>().ToList();

        if (people.Count > 0)
          _core.PeopleM.Select(people, segmentM.Person, e.IsCtrlOn, e.IsShiftOn);

        if (segments.Count > 0)
          SegmentsM.Select(segments, segmentM, e.IsCtrlOn, e.IsShiftOn);
      }
    }

    private async Task CompareAsync() {
      await _workTask.Cancel();
      SegmentsM.AddSegmentsForComparison();
      _core.TitleProgressBarM.ResetProgressBars(SegmentsM.Loaded.Count);
      _core.TitleProgressBarM.IsVisible = true;
      await _workTask.Start(FindSimilaritiesAsync(SegmentsM.Loaded, _workTask.Token));
      _core.TitleProgressBarM.IsVisible = false;
    }

    private Task FindSimilaritiesAsync(List<SegmentM> segments, CancellationToken token) {
      return Task.Run(async () => {
        // clear previous loaded similar
        // clear needs to be for Loaded!
        foreach (var segment in segments) {
          segment.Similar?.Clear();
          segment.SimMax = 0;
        }

        var tm = new Accord.Imaging.ExhaustiveTemplateMatching(0);
        var done = 0;

        foreach (var segmentA in segments) {
          if (token.IsCancellationRequested) break;
          await SetComparePictureAsync(segmentA, SegmentsM.CompareSegmentSize);
          _progress.Report(++done);
        }

        done = 0;
        _progress.Report(done);

        foreach (var segmentA in segments) {
          if (token.IsCancellationRequested) break;
          if (!_compareBitmaps.ContainsKey(segmentA)) {
            _progress.Report(++done);
            continue;
          }

          segmentA.Similar ??= new();
          foreach (var segmentB in segments) {
            if (token.IsCancellationRequested) break;
            if (!_compareBitmaps.ContainsKey(segmentB)) continue;
            // do not compare segment with it self or with segment that have same person
            if (segmentA == segmentB || (segmentA.Person != null && segmentA.Person == segmentB.Person)) continue;
            // do not compare segment with Person.Id > 0 with segment with also Person.Id > 0
            if (segmentA.Person?.Id > 0 && segmentB.Person?.Id > 0) continue;

            var matchings = tm.ProcessImage(_compareBitmaps[segmentB], _compareBitmaps[segmentA]);
            var sim = Math.Round(matchings.Max(x => x.Similarity) * 100, 1);
            if (sim < SegmentsM.SimilarityLimitMin) continue;

            segmentA.Similar.Add(segmentB, sim);
            if (segmentA.SimMax < sim) segmentA.SimMax = sim;
          }

          if (segmentA.Similar.Count == 0) {
            segmentA.Similar = null;
            segmentA.SimMax = 0;
          }

          _progress.Report(++done);
        }
      }, token);
    }

    private async Task SetComparePictureAsync(SegmentM segment, int size) {
      if (_compareBitmaps.ContainsKey(segment)) return;

      var bitmap = await Task.Run(() => {
        try {
          if (!File.Exists(segment.FilePathCache))
            CreateThumbnail(segment);

          return File.Exists(segment.FilePathCache)
            ? Imaging.GetBitmapSource(segment.FilePathCache)?.ToGray().Resize(size).ToBitmap()
            : null;
        }
        catch (Exception ex) {
          Log.Error(ex, segment.FilePathCache);
          return null;
        }
      });

      if (bitmap != null)
        _compareBitmaps.Add(segment, bitmap);
    }

    public void CreateThumbnail(SegmentM segment) {
      var filePath = segment.MediaItem.MediaType == MediaType.Image
        ? segment.MediaItem.FilePath
        : segment.MediaItem.FilePathCache;
      var rect = new Int32Rect(
        (int)segment.X,
        (int)segment.Y,
        (int)segment.Size,
        (int)segment.Size);

      try {
        Imaging.GetCroppedBitmapSource(filePath, rect, SegmentsM.SegmentSize)
          ?.SaveAsJpg(80, segment.FilePathCache);

        SegmentThumbnailSourceConverter.IgnoreImageCacheSegment = segment;
        segment.OnPropertyChanged(nameof(segment.FilePathCache));
      }
      catch (Exception ex) {
        Log.Error(ex, filePath);
      }
    }
  }
}
