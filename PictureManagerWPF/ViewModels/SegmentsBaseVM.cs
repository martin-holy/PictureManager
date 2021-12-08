using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Converters;
using PictureManager.Commands;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.UserControls;
using PictureManager.Views;

namespace PictureManager.ViewModels {
  public class SegmentsBaseVM {
    private readonly Core _core;
    private readonly AppCore _coreVM;
    
    public SegmentsM Model { get; set; }
    public ObservableCollection<Tuple<Int32Rect, bool>> SegmentToolTipRects { get; } = new();
    
    public RelayCommand<SegmentM> SetSegmentPictureCommand { get; }
    public RelayCommand<object> SetSelectedAsSamePersonCommand { get; }
    public RelayCommand<SegmentM> SegmentToolTipRectsReloadCommand { get; }
    public RelayCommand<ClickEventArgs> ViewMediaItemsWithSegmentCommand { get; }
    public RelayCommand<object> SegmentMatchingCommand { get; }

    public SegmentsBaseVM(Core core, AppCore coreVM, SegmentsM model) {
      _core = core;
      _coreVM = coreVM;
      Model = model;

      SetSegmentPictureCommand = new(
        async segment => await segment.SetPictureAsync(core.SegmentsM.SegmentSize),
        segment => segment != null);
      SetSelectedAsSamePersonCommand = new(SetSelectedAsSamePerson);
      SegmentToolTipRectsReloadCommand = new(SegmentToolTipRectsReload);
      ViewMediaItemsWithSegmentCommand = new(ViewMediaItemsWithSegment);
      SegmentMatchingCommand = new(SegmentMatching, () => _core.ThumbnailsGridsM.Current?.FilteredItems.Count > 0);
    }
    
    private void SetSelectedAsSamePerson() {
      Model.SetSelectedAsSamePerson();
      Model.DeselectAll();
      AppCore.OnSetPerson?.Invoke(null, EventArgs.Empty);
    }
    
    private void SegmentToolTipRectsReload(SegmentM segment) {
      // TODO move to model
      SegmentToolTipRects.Clear();
      if (segment?.MediaItem?.Segments == null) return;

      var scale = segment.MediaItem.Width / (double)segment.MediaItem.ThumbWidth;

      foreach (var s in segment.MediaItem.Segments) {
        var sRect = s.ToRect();
        var rect = new Int32Rect((int)(sRect.X / scale), (int)(sRect.Y / scale), (int)(sRect.Width / scale), (int)(sRect.Height / scale));
        SegmentToolTipRects.Add(new(rect, s == segment));
      }
    }

    private void ViewMediaItemsWithSegment(ClickEventArgs e) {
      if (e.ClickCount < 2 || e.DataContext is not SegmentV segmentV || segmentV.Segment?.MediaItem == null) return;
      var segmentM = segmentV.Segment;

      _core.MediaItemsM.Current = segmentM.MediaItem;
      WindowCommands.SwitchToFullScreen();

      List<MediaItemM> items = null;

      if (segmentM.PersonId == 0) {
        if (_coreVM.MainTabsVM.Selected is SegmentMatchingControl
            && Model.LoadedGroupedByPerson.Count > 0
            && Model.LoadedGroupedByPerson[^1].Any(x => x.PersonId == 0)) {
          items = Model.LoadedGroupedByPerson[^1].Select(x => x.MediaItem).Distinct().ToList();
        }
        else
          items = new() { segmentM.MediaItem };
      }
      else {
        items = Model.All.Where(x => x.PersonId == segmentM.PersonId)
          .Select(x => x.MediaItem).Distinct().OrderBy(x => x.FileName).ToList();
      }

      // TODO
      App.WMain.MediaViewer.SetMediaItems(items);
      App.WMain.MediaViewer.SetMediaItemSource(segmentM.MediaItem);
    }

    private void SegmentMatching() {
      var mediaItems = _core.ThumbnailsGridsM.Current.GetSelectedOrAll();
      var control = _coreVM.MainTabsVM.ActivateTab<SegmentMatchingControl>();
      var all = MessageDialog.Show("Segment Matching", "Do you want to load all segments or just segments with person?",
        true, new[] { "All segments", "Segments with person" });

      control?.SetMediaItems(mediaItems);
      _ = control?.LoadSegmentsAsync(!all);
    }
  }
}
