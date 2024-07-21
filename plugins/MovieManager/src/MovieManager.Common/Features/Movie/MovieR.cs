using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MovieManager.Plugins.Common.DTOs;
using PictureManager.Common.Models;
using PictureManager.Common.Models.MediaItems;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using PM = PictureManager.Common;

namespace MovieManager.Common.Features.Movie;

/// <summary>
/// DB fields: Id|Title|Year|YearEnd|Length|Rating|MyRating|Genres|MPAA|Seen|Poster|MediaItems|Keywords|Plot
/// </summary>
public sealed class MovieR(CoreR coreR, PM.Repositories.CoreR pmCoreR) : TableDataAdapter<MovieM>(coreR, "Movies", 14) {
  public static MovieM Dummy { get; } = new(0, string.Empty);
  public event EventHandler<MovieM[]> MoviesKeywordsChangedEvent = delegate { };
  public event EventHandler<MovieM> PosterChangedEvent = delegate { };

  public override MovieM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), csv[1]) {
      Year = csv[2].IntParseOrDefault(0),
      YearEnd = string.IsNullOrEmpty(csv[3]) ? null : int.Parse(csv[3]),
      Length = csv[4].IntParseOrDefault(0),
      Rating = csv[5].IntParseOrDefault(0) / 10.0,
      MyRating = csv[6].IntParseOrDefault(0) / 10.0,
      MPAA = string.IsNullOrEmpty(csv[8]) ? null : csv[8],
      Seen = string.IsNullOrEmpty(csv[9]) ? [] : new ObservableCollection<DateOnly>(csv[9].Split(',').Select(x => DateOnly.ParseExact(x, "yyyyMMdd", CultureInfo.InvariantCulture))),
      Plot = string.IsNullOrEmpty(csv[13]) ? null : csv[13]
    };

  public override string ToCsv(MovieM item) =>
    string.Join("|",
      item.GetHashCode().ToString(),
      item.Title,
      item.Year.ToString(),
      item.YearEnd?.ToString(),
      item.Length.ToString(),
      ((int)(item.Rating * 10)).ToString(),
      ((int)(item.MyRating * 10)).ToString(),
      item.Genres.ToHashCodes().ToCsv(),
      item.MPAA,
      item.Seen.Select(x => x.ToString("yyyyMMdd", CultureInfo.InvariantCulture)).ToCsv(),
      item.Poster?.GetHashCode().ToString(),
      item.MediaItems?.ToHashCodes().ToCsv(),
      item.Keywords?.ToHashCodes().ToCsv(),
      item.Plot);

  public override void LinkReferences() {
    foreach (var (item, csv) in AllCsv) {
      item.Genres = coreR.Genre.LinkList(csv[7], null, this) ?? [];
      item.Poster = pmCoreR.MediaItem.GetById(csv[10], true);
      item.MediaItems = pmCoreR.MediaItem.Link(csv[11]);
      item.Keywords = pmCoreR.Keyword.Link(csv[12], this);
    }
  }

  public MovieM ItemCreate(MovieDetail md) {
    var item = ItemCreate(new MovieM(GetNextId(), md.Title) {
      Year = md.Year,
      YearEnd = md.YearEnd,
      Length = md.Runtime,
      Rating = md.Rating,
      MPAA = md.MPAA,
      Plot = md.Plot
    });

    item.Genres = md.Genres?
      .Select(x => coreR.Genre.GetGenre(x, true))
      .Where(x => x != null)
      .Select(x => x!)
      .ToList() ?? [];

    return item;
  }

  public void AddMediaItems(MovieM movie, MediaItemM[] mediaItems) {
    movie.MediaItems ??= [];
    movie.MediaItems.AddRange(mediaItems.Except(movie.MediaItems));
    IsModified = true;
  }

  public void RemoveMediaItems(MovieM movie, MediaItemM[] mediaItems) {
    if (movie.MediaItems == null) return;

    foreach (var mi in mediaItems)
      movie.MediaItems.Remove(mi);

    IsModified = true;
  }

  public void AddSeenDate(MovieM movie, DateOnly date) {
    movie.Seen.AddInOrder(date, (a, b) => a.CompareTo(b));
    IsModified = true;
  }

  public void RemoveSeenDate(MovieM movie, DateOnly date) {
    movie.Seen.Remove(date);
    IsModified = true;
  }

  public void SetPoster(MovieM movie, MediaItemM mi) {
    movie.Poster = mi;
    AddMediaItems(movie, [mi]);
    IsModified = true;
    PosterChangedEvent(this, movie);
  }

  public void OnMediaItemDeleted(MediaItemM mi) {
    foreach (var movie in All.Where(x => ReferenceEquals(x.Poster, mi))) {
      movie.Poster = null;
      IsModified = true;
    }

    foreach (var movie in All.Where(x => x.MediaItems?.Contains(mi) == true)) {
      movie.MediaItems!.Remove(mi);
      IsModified = true;
    }
  }

  public void OnKeywordDeleted(KeywordM keyword) =>
    ToggleKeyword(All.Where(x => x.Keywords?.Contains(keyword) == true).ToArray(), keyword);

  public void ToggleKeyword(MovieM[] movies, KeywordM keyword) {
    foreach (var movie in movies) {
      movie.Keywords = movie.Keywords.Toggle(keyword);
      IsModified = true;
    }

    MoviesKeywordsChangedEvent(this, movies);
  }
}