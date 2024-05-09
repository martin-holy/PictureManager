using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MovieManager.Common.Models;
using MovieManager.Plugins.Common.Models;
using PictureManager.Interfaces.Models;
using PictureManager.Interfaces.Repositories;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace MovieManager.Common.Repositories;

/// <summary>
/// DB fields: Id|Title|Year|YearEnd|Length|Rating|MyRating|Genres|MPAA|Seen|Poster|MediaItems|Plot
/// </summary>
public sealed class MovieR(CoreR coreR, IPMCoreR pmCoreR) : TableDataAdapter<MovieM>(coreR, "Movies", 13) {
  public override MovieM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), csv[1]) {
      Year = csv[2].IntParseOrDefault(0),
      YearEnd = string.IsNullOrEmpty(csv[3]) ? null : int.Parse(csv[3]),
      Length = csv[4].IntParseOrDefault(0),
      Rating = csv[5].IntParseOrDefault(0) / 10.0,
      MyRating = csv[6].IntParseOrDefault(0) / 10.0,
      MPAA = string.IsNullOrEmpty(csv[8]) ? null : csv[8],
      Seen = string.IsNullOrEmpty(csv[9]) ? [] : new ObservableCollection<DateOnly>(csv[9].Split(',').Select(x => DateOnly.ParseExact(x, "yyyyMMdd", CultureInfo.InvariantCulture))),
      Plot = string.IsNullOrEmpty(csv[12]) ? null : csv[12]
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
      item.Genres?.ToHashCodes().ToCsv(),
      item.MPAA,
      item.Seen.Select(x => x.ToString("yyyyMMdd", CultureInfo.InvariantCulture)).ToCsv(),
      item.Poster?.GetHashCode().ToString(),
      item.MediaItems?.ToHashCodes().ToCsv(),
      item.Plot);

  public override void LinkReferences() {
    foreach (var (item, csv) in AllCsv) {
      item.Genres = coreR.Genre.LinkList(csv[7], null, null);
      item.Poster = pmCoreR.MediaItem.GetById(csv[10], true);
      item.MediaItems = pmCoreR.MediaItem.Link(csv[11]);
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

    item.Genres = md.Genres
      .Select(x => coreR.Genre.GetGenre(x, true))
      .Where(x => x != null)
      .ToList();

    return item;
  }

  public void AddMediaItems(MovieM movie, IMediaItemM[] mediaItems) {
    if (movie == null) return;
    movie.MediaItems ??= [];
    movie.MediaItems.AddRange(mediaItems.Except(movie.MediaItems));
    IsModified = true;
  }

  public void AddSeenDate(MovieM movie, DateOnly date) {
    if (movie == null) return;
    movie.Seen.AddInOrder(date, (a, b) => a.CompareTo(b));
    IsModified = true;
  }

  public void RemoveSeenDate(MovieM movie, DateOnly date) {
    if (movie == null) return;
    movie.Seen.Remove(date);
    IsModified = true;
  }
}