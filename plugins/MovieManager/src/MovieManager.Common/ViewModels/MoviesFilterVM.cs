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
  public List<GenreFilterVM> Genres { get; set; }

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

  private void Update(IReadOnlyCollection<MovieM> movies, IReadOnlyCollection<GenreM> genres) {
    var zeroItems = movies.Count == 0;

    if (zeroItems) {
      Year.Zero();
      Length.Zero();
      Rating.Zero();
      MyRating.Zero();
    }
    else {
      Year.Reset(movies.Min(x => x.Year), movies.Max(x => x.Year));
      Length.Reset(movies.Min(x => x.Length), movies.Max(x => x.Length));
      Rating.Reset(movies.Min(x => x.Rating), movies.Max(x => x.Rating));
      MyRating.Reset(movies.Min(x => x.MyRating), movies.Max(x => x.MyRating));
    }

    Genres.Clear();
    Genres.AddRange(genres.Select(x => new GenreFilterVM(x)));
  }

  public bool Filter(MovieM movie) {
    if (!Year.IsFullRange && !Year.Fits(movie.Year)
        || !Length.IsFullRange && !Length.Fits(movie.Length)
        || !Rating.IsFullRange && !Rating.Fits(movie.Rating)
        || !MyRating.IsFullRange && !MyRating.Fits(movie.MyRating))
      return false;

    return true;
  }

  public void Open(IReadOnlyCollection<MovieM> movies, IReadOnlyCollection<GenreM> genres) {
    Update(movies, genres);
  }
}