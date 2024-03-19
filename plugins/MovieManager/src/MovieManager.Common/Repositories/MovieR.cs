using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MovieManager.Common.Models;
using PictureManager.Plugins.Common.Interfaces.Repositories;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MovieManager.Common.Repositories;

/// <summary>
/// DB fields: ID|Title|Year|Length|Rating|PersonalRating|Genre|SubGenres|Actors|Keywords|SeenWhen|MPAA|Plot|Cover|MediaItems
/// </summary>
public sealed class MovieR(CoreR coreR, IPluginHostCoreR phCoreR) : TableDataAdapter<MovieM>(coreR, "Movies", 15) {
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
      m.Plot ?? string.Empty,
      m.Cover?.GetHashCode().ToString() ?? string.Empty,
      m.MediaItems.ToHashCodes().ToCsv());

  public override void LinkReferences() {
    foreach (var (m, csv) in AllCsv) {
      m.Genre = coreR.Genre.GetById(csv[6], true);
      m.SubGenres = coreR.Genre.LinkList(csv[7], null, null);
      m.Actors = phCoreR.Person.Link(csv[8], this);
      m.Keywords = phCoreR.Keyword.Link(csv[9], this);
      m.Cover = phCoreR.MediaItem.GetById(csv[13], true);
      m.MediaItems = phCoreR.MediaItem.Link(csv[14], this);
    }
  }

  public void ImportFromJson() {
    var filePath = Path.Combine("plugins", "MovieManager", "MoviesExport_50.json");
    if (!File.Exists(filePath)) {
      Log.Error("Import movies from JSON", $"File not found. {filePath}");
      return;
    }

    try {
      var movies = JsonSerializer.Deserialize<JsonMovies>(File.ReadAllText(filePath));
      foreach (var movie in movies.Movies) {

        ItemCreate(new(GetNextId(), movie.Title) {
          Year = movie.MovieYear,
          Length = movie.Length,
          Rating = movie.Rating,
          PersonalRating = movie.PersonalRating,
          MPAA = movie.MPAA,
          Plot = movie.Plot
        });
        /*
         *public int Id { get; }
           public string Title { get; set; }
           public int Year { get; set; }
           public int Length { get; set; }
           public double Rating { get; set; }
           public double PersonalRating { get; set; }
           public GenreM Genre { get; set; }
           public List<GenreM> SubGenres { get; set; }
           public List<IPluginHostPersonM> Actors { get; set; }
           public List<IPluginHostKeywordM> Keywords { get; set; }
           public DateOnly[] SeenWhen { get; set; }
           public string MPAA { get; set; }
           public string Plot { get; set; }
         */
      }
    }
    catch (Exception ex) {
      Log.Error(ex);
    }
  }
}

public class JsonMovies {
  public JsonMovie[] Movies { get; set; }
}

public class JsonMovie {
  public int MovieID { get; set; }
  public string Title { get; set; }
  public string Genere { get; set; }
  public string Subgenre { get; set; }
  public int MovieYear { get; set; }
  public int Length { get; set; }
  public string MPAA { get; set; }
  public string Plot { get; set; }
  public string Cover { get; set; }
  public double Rating { get; set; }
  public int PersonalRating { get; set; }
  public string SeenWhen { get; set; }
  public string DateInsert { get; set; }
  public string KeyWords { get; set; }
}