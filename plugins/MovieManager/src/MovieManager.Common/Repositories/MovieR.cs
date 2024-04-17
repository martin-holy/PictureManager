using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MovieManager.Common.Models;
using MovieManager.Plugins.Common.Interfaces;
using PictureManager.Interfaces.Repositories;
using System;
using System.Globalization;
using System.Linq;

namespace MovieManager.Common.Repositories;

/// <summary>
/// DB fields: Id|Title|Year|YearEnd|Length|Rating|PersonalRating|Genres|MPAA|SeenWhen|Poster|Plot
/// </summary>
public sealed class MovieR(CoreR coreR, ICoreR phCoreR) : TableDataAdapter<MovieM>(coreR, "Movies", 12) {
  public override MovieM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), csv[1]) {
      Year = csv[2].IntParseOrDefault(0),
      YearEnd = string.IsNullOrEmpty(csv[3]) ? null : int.Parse(csv[3]),
      Length = csv[4].IntParseOrDefault(0),
      Rating = csv[5].IntParseOrDefault(0) / 10.0,
      PersonalRating = csv[6].IntParseOrDefault(0) / 10.0,
      MPAA = string.IsNullOrEmpty(csv[8]) ? null : csv[8].Split(','),
      SeenWhen = string.IsNullOrEmpty(csv[9]) ? null : csv[9].Split(',').Select(x => DateOnly.ParseExact(x, "yyyyMMdd", CultureInfo.InvariantCulture)).ToArray(),
      Plot = string.IsNullOrEmpty(csv[11]) ? null : csv[11]
    };

  public override string ToCsv(MovieM item) =>
    string.Join("|",
      item.GetHashCode().ToString(),
      item.Title,
      item.Year.ToString(),
      item.YearEnd?.ToString() ?? string.Empty,
      item.Length.ToString(),
      ((int)(item.Rating * 10)).ToString(),
      ((int)(item.PersonalRating * 10)).ToString(),
      item.Genres?.ToHashCodes().ToCsv() ?? string.Empty,
      item.MPAA?.ToCsv() ?? string.Empty,
      item.SeenWhen?.Select(x => x.ToString("yyyyMMdd", CultureInfo.InvariantCulture)).ToCsv() ?? string.Empty,
      item.Poster?.GetHashCode().ToString(),
      item.Plot ?? string.Empty);

  public override void LinkReferences() {
    foreach (var (item, csv) in AllCsv) {
      item.Genres = coreR.Genre.LinkList(csv[7], null, null);
      item.Poster = phCoreR.MediaItem.GetById(csv[10], true);
    }
  }

  public MovieM ItemCreate(IMovieDetail md) {
    var item = ItemCreate(new MovieM(GetNextId(), md.Title) {
      Year = md.Year,
      YearEnd = md.YearEnd,
      Length = md.Length,
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
}