using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Interfaces.Models;
using System;
using System.Collections.Generic;

namespace MovieManager.Common.Models;

public sealed class MovieM : ObservableObject, ISelectable {
  private bool _isSelected;
  private double _rating;
  private double _myRating;

  public int Id { get; }
  public string Title { get; set; }
  public int Year { get; set; }
  public int? YearEnd { get; set; }
  public int Length { get; set; }
  public double Rating { get => _rating; set { _rating = value; OnPropertyChanged(); } }
  public double MyRating { get => _myRating; set { _myRating = value; OnPropertyChanged(); } }
  public List<GenreM> Genres { get; set; }
  public List<IKeywordM> Keywords { get; set; }
  public DateOnly[] SeenWhen { get; set; }
  public string MPAA { get; set; }
  public string Plot { get; set; }
  public IMediaItemM Poster { get; set; }
  public List<IMediaItemM> MediaItems { get; set; }
  public MovieDetailIdM DetailId { get; set; }

  public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }

  public string FormatedLength =>
    TimeSpan.FromMinutes(Length).ToString(@"h\:mm");

  public MovieM(int id, string title) {
    Id = id;
    Title = title;
  }

  public override int GetHashCode() => Id;
}