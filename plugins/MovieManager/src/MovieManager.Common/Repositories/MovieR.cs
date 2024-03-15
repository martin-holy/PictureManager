using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MovieManager.Common.Models;
using System;
using System.Globalization;
using System.Linq;

namespace MovieManager.Common.Repositories;

/// <summary>
/// DB fields: ID|Title|Year|Length|Rating|PersonalRating|Genre|SubGenres|Actors|Keywords|SeenWhen|MPAA|Plot
/// </summary>
public sealed class MovieR : TableDataAdapter<MovieM> {
  private readonly CoreR _coreR;

  public MovieR(CoreR coreR) : base("Movies", 13) {
    _coreR = coreR;
  }

  public override MovieM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), csv[1]) {
      Year = csv[2].IntParseOrDefault(0),
      Length = csv[3].IntParseOrDefault(0),
      Rating = csv[4].IntParseOrDefault(0) / 10.0,
      PersonalRating = csv[5].IntParseOrDefault(0) / 10.0,
      SeenWhen = string.IsNullOrEmpty(csv[10]) ? null : csv[10].Split(',').Select(x => DateOnly.ParseExact(x, "yyyyMMdd", CultureInfo.InvariantCulture)).ToArray(),
      MPAA = string.IsNullOrEmpty(csv[11]) ? null : csv[11],
      Plot = string.IsNullOrEmpty(csv[12]) ? null : csv[12]
    };

  public override string ToCsv(MovieM m) =>
    string.Join("|",
      m.GetHashCode().ToString(),
      m.Title ?? string.Empty,
      m.Year.ToString(),
      m.Length.ToString(),
      ((int)(m.Rating * 10)).ToString(),
      ((int)(m.PersonalRating * 10)).ToString(),
      m.Genre?.GetHashCode().ToString() ?? string.Empty,
      m.SubGenres.ToHashCodes().ToCsv(),
      m.Actors.ToHashCodes().ToCsv(),
      m.Keywords.ToHashCodes().ToCsv(),
      m.SeenWhen?.Select(x => x.ToString("yyyyMMdd", CultureInfo.InvariantCulture)).ToCsv() ?? string.Empty,
      m.MPAA ?? string.Empty,
      m.Plot ?? string.Empty);

  public override void LinkReferences() {
    foreach (var (m, csv) in AllCsv) {
      m.Genre = _coreR.Genre.GetById(csv[6], true);
      // TODO SubGenre, Actors and Keywords
    }
  }
}