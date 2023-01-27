using MH.Utils.Dialogs;
using System;
using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;

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

    private SegmentEditMode _editMode;
    private bool _isCurrentModified;

    public SegmentsM SegmentsM { get; }
    public SegmentRectM Current { get; set; }
    public ObservableCollection<SegmentRectM> MediaItemSegmentsRects { get; } = new();

    public SegmentsRectsM(SegmentsM segmentsM) {
      SegmentsM = segmentsM;
    }

    public void SetCurrent(SegmentRectM current, double x, double y) {
      Current = current;
      _editMode = GetEditMode(Current, x, y);
      SegmentsM.DeselectAll();
      SegmentsM.SetSelected(current.Segment, true);
    }

    private static SegmentEditMode GetEditMode(SegmentRectM current, double x, double y) {
      var xDiff = Math.Abs(current.X + current.Radius - x);
      var yDiff = Math.Abs(current.Y + current.Radius - y);

      if (xDiff < 10 && yDiff < 10)
        return SegmentEditMode.Move;

      if (Math.Abs(xDiff - yDiff) < 10)
        return SegmentEditMode.ResizeCorner;
      else
        return SegmentEditMode.ResizeEdge;
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
      var (newX, newY) = ConvertPos(x, y, Scale, MediaItem);
      var segment = SegmentsM.AddNewSegment(newX, newY, 0, MediaItem);
      _editMode = SegmentEditMode.ResizeCorner;
      _isCurrentModified = true;
      Current = new(segment, Scale);
      MediaItemSegmentsRects.Add(Current);
    }

    public static (int, int) ConvertPos(double x, double y, double scale, MediaItemM mediaItem, bool reverseMode = false) {
      double rX;
      double rY;

      switch ((MediaOrientation)mediaItem.Orientation) {
        case MediaOrientation.Rotate180:
          rX = mediaItem.Width * scale - x;
          rY = mediaItem.Height * scale - y;
          break;
        case MediaOrientation.Rotate270:
          if (reverseMode) {
            rX = mediaItem.Height * scale - y;
            rY = x;
          }
          else {
            rX = y;
            rY = mediaItem.Height * scale - x;
          }
          break;
        case MediaOrientation.Rotate90:
          if (reverseMode) {
            rX = y;
            rY = mediaItem.Width * scale - x;
          }
          else {
            rX = mediaItem.Width * scale - y;
            rY = x;
          }
          break;
        default:
          rX = x;
          rY = y;
          break;
      }

      return ((int)(rX / scale), (int)(rY / scale));
    }

    public void StartEdit(int x, int y) {
      var centerX = Current.X + Current.Radius;
      var centerY = Current.Y + Current.Radius;

      _isCurrentModified = true;
      IsEditOn = true;

      switch (_editMode) {
        case SegmentEditMode.Move:
        Current.X = x;
        Current.Y = y;
        break;

        case SegmentEditMode.ResizeEdge:
        var newCenterX = centerX;
        var newCenterY = centerY;
        var newRadius = Current.Radius;
        var xHalfDiff = (x - Current.X) / 2;
        var yHalfDiff = (y - Current.Y) / 2;
        var lDiff = Math.Abs(x - Current.X);
        var rDiff = Math.Abs(x - Current.X - Current.Size);
        var tDiff = Math.Abs(y - Current.Y);
        var bDiff = Math.Abs(y - Current.Y - Current.Size);
        var minDiff = (new double[] { lDiff, rDiff, tDiff, bDiff }).Min();

        if (lDiff == minDiff) {
          // left edge
          newRadius = Current.Radius - xHalfDiff;
          newCenterX = x + newRadius;
        }
        else if (bDiff == minDiff) {
          // bottom edge
          newRadius = yHalfDiff;
          newCenterY = Current.Y + newRadius;
        }
        else if (tDiff == minDiff) {
          // top edge
          newRadius = Current.Radius - yHalfDiff;
          newCenterY = y + newRadius;
        }
        else if (rDiff == minDiff) {
          // right edge
          newRadius = xHalfDiff;
          newCenterX = Current.X + newRadius;
        }

        Current.X = newCenterX;
        Current.Y = newCenterY;
        Current.Radius = newRadius;

        break;

        case SegmentEditMode.ResizeCorner:
        Current.Radius = Math.Max(Math.Abs(centerX - x), Math.Abs(centerY - y));
        break;
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
