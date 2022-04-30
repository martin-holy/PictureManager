using MH.Utils.Dialogs;
using System;
using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PictureManager.Domain.Models {
  public sealed class SegmentsRectsM : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged = delegate { };
    public void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged(this, new(name));

    private double _scale;
    private bool _isEditOn;
    private MediaItemM _mediaItemM;

    public double Scale {
      get => _scale;
      set {
        _scale = value;
        OnPropertyChanged();
        UpdateScale();
      }
    }
    public bool IsEditOn { get => _isEditOn; set { _isEditOn = value; OnPropertyChanged(); } }

    public MediaItemM MediaItem {
      get => _mediaItemM;
      set {
        _mediaItemM = value;
        OnPropertyChanged();
        ReloadMediaItemSegmentRects();
      }
    }

    private bool _isEditModeMove;
    private bool _isCurrentModified;

    public SegmentsM SegmentsM { get; }
    public SegmentRectM Current { get; set; }
    public ObservableCollection<SegmentRectM> MediaItemSegmentsRects { get; } = new();

    public SegmentsRectsM(SegmentsM segmentsM) {
      SegmentsM = segmentsM;
    }

    public void SetCurrent(SegmentRectM current, bool isEditModeMove) {
      Current = current;
      _isEditModeMove = isEditModeMove;
      SegmentsM.DeselectAll();
      SegmentsM.SetSelected(current.Segment, true);
    }

    private void ReloadMediaItemSegmentRects() {
      Current = null;
      MediaItemSegmentsRects.Clear();
      if (MediaItem?.Segments == null) return;

      SegmentsM.DeselectAll();

      foreach (var segment in MediaItem.Segments)
        MediaItemSegmentsRects.Add(new(segment, Scale));
    }

    private void UpdateScale() {
      foreach (var sr in MediaItemSegmentsRects)
        sr.Scale = Scale;
    }

    public void CreateNew(double x, double y) {
      var segment = SegmentsM.AddNewSegment((int)(x / Scale), (int)(y / Scale), 0, MediaItem);
      _isEditModeMove = false;
      _isCurrentModified = true;
      Current = new(segment, Scale);
      MediaItemSegmentsRects.Add(Current);
    }

    public void StartEdit(int x, int y) {
      _isCurrentModified = true;
      IsEditOn = true;

      if (_isEditModeMove) {
        Current.X = x;
        Current.Y = y;
      }
      else {
        var centerX = Current.X + Current.Radius;
        var centerY = Current.Y + Current.Radius;
        Current.Radius = Math.Max(Math.Abs(centerX - x), Math.Abs(centerY - y));
      }
    }

    public void EndEdit() {
      if (Current == null) {
        SegmentsM.DeselectAll();
        return;
      }

      if (_isCurrentModified) {
        SegmentsM.DataAdapter.IsModified = true;
        File.Delete(Current.Segment.FilePathCache);
        Current.Segment.OnPropertyChanged(nameof(Current.Segment.FilePathCache));
        _isCurrentModified = false;
        IsEditOn = false;
      }

      Current = null;
    }

    public void Delete(SegmentRectM item) {
      if (Core.DialogHostShow(new MessageDialog(
        "Delete Segment",
        "Do you really want to delete this segment?",
        Res.IconQuestion,
        true)) != 0) return;

      SegmentsM.Delete(item.Segment);
      MediaItemSegmentsRects.Remove(item);
    }
  }
}
