using MH.Utils.BaseClasses;
using MovieManager.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MovieManager.Common.ViewModels;

public sealed class MoviesFilterVM : ObservableObject {
  public SelectionRange Year { get; } = new();
  public SelectionRange Length { get; } = new();
  public SelectionRange Rating { get; } = new();
  public SelectionRange MyRating { get; } = new();

  public event EventHandler FilterChangedEvent = delegate { };

  public RelayCommand ClearCommand { get; }

  public MoviesFilterVM() {
    Year.ChangedEvent += OnRangeChanged;
    Length.ChangedEvent += OnRangeChanged;
    Rating.ChangedEvent += OnRangeChanged;
    MyRating.ChangedEvent += OnRangeChanged;

    ClearCommand = new(Clear);
  }

  private void RaiseFilterChanged() => FilterChangedEvent(this, EventArgs.Empty);

  private void OnRangeChanged(object sender, EventArgs e) => RaiseFilterChanged();

  private void Clear() {
    Year.SetFullRange();
    Length.SetFullRange();
    Rating.SetFullRange();
    MyRating.SetFullRange();

    RaiseFilterChanged();
  }

  private void Update(IReadOnlyCollection<MovieM> limit) {
    var zeroItems = !limit.Any();

    if (zeroItems) {
      Year.Zero();
      Length.Zero();
      Rating.Zero();
      MyRating.Zero();
    }
    else {
      Year.Reset(limit.Min(x => x.Year), limit.Max(x => x.Year));
      Length.Reset(limit.Min(x => x.Length), limit.Max(x => x.Length));
      Rating.Reset(limit.Min(x => x.Rating), limit.Max(x => x.Rating));
      MyRating.Reset(limit.Min(x => x.MyRating), limit.Max(x => x.MyRating));
    }
  }

  public bool Filter(MovieM movie) {
    if (!Year.IsFullRange && !Year.Fits(movie.Year)
        || !Length.IsFullRange && !Length.Fits(movie.Length)
        || !Rating.IsFullRange && !Rating.Fits(movie.Rating)
        || !MyRating.IsFullRange && !MyRating.Fits(movie.MyRating))
      return false;

    return true;
  }

  public void Open(IReadOnlyCollection<MovieM> movies) {
    Update(movies);
  }
}