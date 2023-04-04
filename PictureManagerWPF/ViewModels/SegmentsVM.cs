using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using MH.UI.WPF.Controls;
using MH.UI.WPF.Converters;
using MH.UI.WPF.Utils;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.EventsArgs;
using MH.Utils.HelperClasses;
using PictureManager.Converters;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using static MH.Utils.DragDropHelper;

namespace PictureManager.ViewModels {
  public sealed class SegmentsVM : ObservableObject {
    private readonly Core _core;
    private readonly AppCore _coreVM;
    private readonly IProgress<int> _progress;
    private readonly Dictionary<SegmentM, System.Drawing.Bitmap> _compareBitmaps = new();
    private VirtualizingWrapPanel _matchingPanel;
    private VirtualizingWrapPanel _confirmedMatchingPanel;
    private bool _reWrapLoadedItems;
    private bool _reWrapConfirmedItems;

    private readonly WorkTask _workTask = new();

    public SegmentsM SegmentsM { get; }
    public SegmentsRectsVM SegmentsRectsVM { get; }

    public CanDragFunc CanDragFunc { get; }

    public bool ReWrapLoadedItems { get => _reWrapLoadedItems; set { _reWrapLoadedItems = value; OnPropertyChanged(); } }
    public bool ReWrapConfirmedItems { get => _reWrapConfirmedItems; set { _reWrapConfirmedItems = value; OnPropertyChanged(); } }

    public RelayCommand<ClickEventArgs> SelectCommand { get; }
    public RelayCommand<object> CompareCommand { get; }
    public RelayCommand<object> PanelLoadedWidthChangedCommand { get; }
    public RelayCommand<object> PanelConfirmedWidthChangedCommand { get; }

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

      CanDragFunc = CanDrag;

      SelectCommand = new(Select);
      CompareCommand = new(async () => {
        await CompareAsync();
        SegmentsM.Reload(true, true);
      });
      PanelLoadedWidthChangedCommand = new(
        () => ReWrapLoadedItems = true,
        () => !_core.MainWindowM.IsFullScreenIsChanging);
      PanelConfirmedWidthChangedCommand = new(() => ReWrapConfirmedItems = true);

      // TODO do it just when needed
      foreach (var person in App.Core.PeopleM.DataAdapter.All.Values)
        person.UpdateDisplayKeywords();
    }

    private object CanDrag(object source) =>
      source is SegmentM segmentM
        ? SegmentsM.GetOneOrSelected(segmentM)
        : null;

    private void Select(ClickEventArgs e) {
      if (e.OriginalSource is Image { DataContext: SegmentM segmentM } image) {
        var rowStackPanel = image.TryFindParent<StackPanel>();
        var wrapPanel = image.TryFindParent<VirtualizingWrapPanel>();
        var rowIndex = wrapPanel.WrappedItems.IndexOf(rowStackPanel.DataContext);
        var itemsGroup = wrapPanel.WrappedItems
          .OfType<ItemsGroup>()
          .LastOrDefault(x => wrapPanel.WrappedItems.IndexOf(x) < rowIndex);
        var list = itemsGroup != null
          ? itemsGroup.Items.Cast<SegmentM>().ToList()
          : wrapPanel.WrappedItems
             .OfType<VirtualizingWrapPanelRow>()
             .Where(x => wrapPanel.WrappedItems.IndexOf(x) <= rowIndex)
             .SelectMany(x => x.Items.Cast<SegmentM>())
             .ToList();

        SegmentsM.Select(list, segmentM, e.IsCtrlOn, e.IsShiftOn);
      }
    }

    private async Task CompareAsync() {
      await _workTask.Cancel();
      SegmentsM.AddSegmentsForComparison();
      _core.TitleProgressBarM.ResetProgressBars(SegmentsM.Loaded.Count);
      await _workTask.Start(FindSimilaritiesAsync(SegmentsM.Loaded, _workTask.Token));
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
          Core.Instance.LogError(ex, segment.FilePathCache);
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
        (int)(segment.X - segment.Radius),
        (int)(segment.Y - segment.Radius),
        (int)segment.Radius * 2,
        (int)segment.Radius * 2);

      try {
        Imaging.GetCroppedBitmapSource(filePath, rect, SegmentsM.SegmentSize)
          ?.SaveAsJpg(80, segment.FilePathCache);

        SegmentThumbnailSourceConverter.IgnoreImageCacheSegment = segment;
        segment.OnPropertyChanged(nameof(segment.FilePathCache));
      }
      catch (Exception ex) {
        _core.LogError(ex, filePath);
      }
    }
  }
}
