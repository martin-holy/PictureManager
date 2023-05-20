using MH.Utils.BaseClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PictureManager.Domain.Models {
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
    public RelayCommand<object> ClearCommand { get; }
    public RelayCommand<object> SizeChangedCommand { get; }

    public MediaItemsFilterM() {
      SetAndCommand = new(item => Set(item, DisplayFilter.And));
      SetOrCommand = new(item => Set(item, DisplayFilter.Or));
      SetNotCommand = new(item => Set(item, DisplayFilter.Not));
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
      if (!ShowImages && mi.MediaType == MediaType.Image) return false;
      if (!ShowVideos && mi.MediaType == MediaType.Video) return false;

      // TODO GeoNames

      //Ratings
      var chosenRatings = FilterOr.OfType<RatingM>().Select(x => x.Value);
      if (chosenRatings.Any() && !chosenRatings.Contains(mi.Rating)) return false;

      // MediaItemSizes
      if ((!Width.MaxSelection() && !Width.Fits(mi.Width))
        || (!Height.MaxSelection() && !Height.Fits(mi.Height))
        || (!Size.MaxSelection() && !Size.Fits((mi.Width * mi.Height) / 1000000.0)))
        return false;

      // People
      var miPeople = new List<PersonM>();
      if (mi.People != null)
        miPeople.AddRange(mi.People);
      if (mi.Segments != null)
        miPeople.AddRange(mi.Segments.Where(x => x.Person != null).Select(x => x.Person));

      var notPeople = FilterNot.OfType<PersonM>();
      if (notPeople.Any() && miPeople.Any() && notPeople.Any(fp => miPeople.Any(p => p == fp))) return false;
      var andPeople = FilterAnd.OfType<PersonM>();
      if (andPeople.Any() && (!miPeople.Any() || !andPeople.All(fp => miPeople.Any(p => p == fp)))) return false;
      var orPeople = FilterOr.OfType<PersonM>();
      if (orPeople.Any() && (!miPeople.Any() || !orPeople.Any(fp => miPeople.Any(p => p == fp)))) return false;

      // Keywords
      var miKeywords = new List<KeywordM>();
      miKeywords.AddRange(miPeople.Where(x => x.Keywords != null).SelectMany(x => x.Keywords));
      if (mi.Keywords != null)
        miKeywords.AddRange(mi.Keywords);
      if (mi.Segments != null)
        miKeywords.AddRange(mi.Segments.Where(x => x.Keywords != null).SelectMany(x => x.Keywords));

      var notKeywords = FilterNot.OfType<KeywordM>();
      if (notKeywords.Any() && miKeywords.Any() && notKeywords.Any(fk => miKeywords.Any(mik => mik.FullName.StartsWith(fk.FullName)))) return false;
      var andKeywords = FilterAnd.OfType<KeywordM>();
      if (andKeywords.Any() && (!miKeywords.Any() || !andKeywords.All(fk => miKeywords.Any(mik => mik.FullName.StartsWith(fk.FullName))))) return false;
      var orKeywords = FilterOr.OfType<KeywordM>();
      if (orKeywords.Any() && (!miKeywords.Any() || !orKeywords.Any(fk => miKeywords.Any(mik => mik.FullName.StartsWith(fk.FullName))))) return false;

      return true;
    }

    public void UpdateSizeRanges(IEnumerable<MediaItemM> limit, bool maxSelection) {
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
}
