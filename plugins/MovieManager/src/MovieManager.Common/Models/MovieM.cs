using MH.Utils.BaseClasses;
using PictureManager.Plugins.Common.Interfaces.Models;
using System;
using System.Collections.Generic;

namespace MovieManager.Common.Models;

public sealed class MovieM : ObservableObject {
  public int Id { get; }
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

  public MovieM(int id, string title) {
    Id = id;
    Title = title;
  }

  public override int GetHashCode() => Id;
}