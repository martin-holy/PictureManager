using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MovieManager.Common.Models;
using MovieManager.Plugins.Common.Models;
using PictureManager.Interfaces.Models;
using PictureManager.Interfaces.Repositories;
using System;
using System.Globalization;
using System.Linq;

namespace MovieManager.Common.Repositories;

/// <summary>
/// DB fields: Id|Title|Year|YearEnd|Length|Rating|MyRating|Genres|MPAA|SeenWhen|Poster|MediaItems|Plot
/// </summary>
public sealed class MovieR(CoreR coreR, ICoreR phCoreR) : TableDataAdapter<MovieM>(coreR, "Movies", 13) {
  public override MovieM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), csv[1]) {
      Year = csv[2].IntParseOrDefault(0),
      YearEnd = string.IsNullOrEmpty(csv[3]) ? null : int.Parse(csv[3]),
      Length = csv[4].IntParseOrDefault(0),
      Rating = csv[5].IntParseOrDefault(0) / 10.0,
      MyRating = csv[6].IntParseOrDefault(0) / 10.0,
      MPAA = string.IsNullOrEmpty(csv[8]) ? null : csv[8],
      SeenWhen = string.IsNullOrEmpty(csv[9]) ? null : csv[9].Split(',').Select(x => DateOnly.ParseExact(x, "yyyyMMdd", CultureInfo.InvariantCulture)).ToArray(),
      Plot = string.IsNullOrEmpty(csv[12]) ? null : csv[12]
    };

  public override string ToCsv(MovieM item) =>
    string.Join("|",
      item.GetHashCode().ToString(),
      item.Title,
      item.Year.ToString(),
      item.YearEnd?.ToString() ?? string.Empty,
      item.Length.ToString(),
      ((int)(item.Rating * 10)).ToString(),
      ((int)(item.MyRating * 10)).ToString(),
      item.Genres?.ToHashCodes().ToCsv() ?? string.Empty,
      item.MPAA ?? string.Empty,
      item.SeenWhen?.Select(x => x.ToString("yyyyMMdd", CultureInfo.InvariantCulture)).ToCsv() ?? string.Empty,
      item.Poster?.GetHashCode().ToString(),
      item.MediaItems?.ToHashCodes().ToCsv() ?? string.Empty,
      item.Plot ?? string.Empty);

  public override void LinkReferences() {
    foreach (var (item, csv) in AllCsv) {
      item.Genres = coreR.Genre.LinkList(csv[7], null, null);
      item.Poster = phCoreR.MediaItem.GetById(csv[10], true);
      item.MediaItems = phCoreR.MediaItem.Link(csv[11]);
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
    movie.MediaItems ??= [];
    movie.MediaItems.AddRange(mediaItems.Except(movie.MediaItems));
    IsModified = true;
  }
}