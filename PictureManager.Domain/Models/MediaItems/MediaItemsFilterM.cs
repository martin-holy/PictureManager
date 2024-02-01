using MH.Utils.BaseClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PictureManager.Domain.Models.MediaItems;

public sealed class MediaItemsFilterM : ObservableObject {
  private bool _showImages = true;
  private bool _showVideos = true;

  public bool ShowImages { get => _showImages; set { _showImages = value; OnFilterChanged(); OnPropertyChanged(); } }
  public bool ShowVideos { get => _showVideos; set { _showVideos = value; OnFilterChanged(); OnPropertyChanged(); } }
  public ObservableCollection<object> FilterAnd { get; } = new();
  public ObservableCollection<object> FilterOr { get; } = new();
  public ObservableCollection<object> FilterNot { get; } = new();
  public SelectionRange Height { get; } = new();
  public SelectionRange Width { get; } = new();
  public SelectionRange Size { get; } = new();

  public event EventHandler FilterChangedEventHandler = delegate { };

  public RelayCommand<object> SetAndCommand { get; }
  public RelayCommand<object> SetOrCommand { get; }
  public RelayCommand<object> SetNotCommand { get; }
  public RelayCommand ClearCommand { get; }
  public RelayCommand SizeChangedCommand { get; }

  public MediaItemsFilterM() {
    SetAndCommand = new(item => Set(item, DisplayFilter.And), null, "Filter And");
    SetOrCommand = new(item => Set(item, DisplayFilter.Or), null, "Filter Or");
    SetNotCommand = new(item => Set(item, DisplayFilter.Not), null, "Filter Not");
    ClearCommand = new(Clear);
    SizeChangedCommand = new(OnFilterChanged);
  }

  private void OnFilterChanged() {
    FilterChangedEventHandler(this, EventArgs.Empty);
  }

  private void Clear() {
    FilterAnd.Clear();
    FilterOr.Clear();
    FilterNot.Clear();

    Size.CoerceValues(true);
    Height.CoerceValues(true);
    Width.CoerceValues(true);

    OnFilterChanged();
  }

  public void Set(object item, DisplayFilter displayFilter) {
    if (FilterAnd.Contains(item) || FilterOr.Contains(item) || FilterNot.Contains(item))
      displayFilter = DisplayFilter.None;

    switch (displayFilter) {
      case DisplayFilter.None:
        FilterAnd.Remove(item);
        FilterOr.Remove(item);
        FilterNot.Remove(item);
        break;
      case DisplayFilter.And:
        FilterAnd.Add(item);
        break;
      case DisplayFilter.Or:
        FilterOr.Add(item);
        break;
      case DisplayFilter.Not:
        FilterNot.Add(item);
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(displayFilter), displayFilter, null);
    }

    OnFilterChanged();
  }

  public bool Filter(MediaItemM mi) {
    // Media Type
    if (!ShowImages && mi is ImageM) return false;
    if (!ShowVideos && mi is VideoM) return false;

    // GeoNames
    if (!Filter(mi.GetGeoNames().ToArray())) return false;

    //Ratings
    var chosenRatings = FilterOr.OfType<RatingM>().Select(x => x.Value).ToArray();
    if (chosenRatings.Any() && !chosenRatings.Contains(mi.Rating)) return false;

    // MediaItemSizes
    if (!Width.MaxSelection() && !Width.Fits(mi.Width)
        || !Height.MaxSelection() && !Height.Fits(mi.Height)
        || !Size.MaxSelection() && !Size.Fits(mi.Width * mi.Height / 1000000.0))
      return false;

    // People
    var miPeople = mi.GetPeople().ToArray();
    if (!Filter(miPeople)) return false;

    // Keywords
    return Filter(mi.GetKeywords().Concat(miPeople.GetKeywords()).Distinct().ToArray());
  }

  private bool Filter<T>(T[] miI) where T : class {
    var notI = FilterNot.OfType<T>().ToArray();
    if (notI.Any() && miI.Any() && notI.Any(fx => miI.Any(x => ReferenceEquals(x, fx)))) return false;
    var andI = FilterAnd.OfType<T>().ToArray();
    if (andI.Any() && (!miI.Any() || !andI.All(fx => miI.Any(x => ReferenceEquals(x, fx))))) return false;
    var orI = FilterOr.OfType<T>().ToArray();
    if (orI.Any() && (!miI.Any() || !orI.Any(fx => miI.Any(x => ReferenceEquals(x, fx))))) return false;

    return true;
  }

  public void UpdateSizeRanges(IList<MediaItemM> limit, bool maxSelection) {
    var zeroItems = !limit.Any();

    if (zeroItems) {
      Size.Zero();
      Height.Zero();
      Width.Zero();
    }
    else {
      Size.Min = Math.Round(limit.Min(x => x.Width * x.Height) / 1000000.0, 1);
      Size.Max = Math.Round((limit.Max(x => x.Width * x.Height) + 100000) / 1000000.0, 1);
      Size.CoerceValues(maxSelection);

      Height.Min = limit.Min(x => x.Height);
      Height.Max = limit.Max(x => x.Height);
      Height.CoerceValues(maxSelection);

      Width.Min = limit.Min(x => x.Width);
      Width.Max = limit.Max(x => x.Width);
      Width.CoerceValues(maxSelection);
    }
  }
}