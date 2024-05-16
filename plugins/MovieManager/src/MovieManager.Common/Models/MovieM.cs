using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Common.Interfaces;
using PictureManager.Common.Models;
using PictureManager.Common.Models.MediaItems;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MovieManager.Common.Models;

public sealed class MovieM : ObservableObject, ISelectable, IHaveKeywords {
  private bool _isSelected;
  private double _myRating;
  private MediaItemM _poster;

  public int Id { get; }
  public string Title { get; set; }
  public int Year { get; set; }
  public int? YearEnd { get; set; }
  public int Length { get; set; }
  public double Rating { get; set; }
  public double MyRating { get => _myRating; set { _myRating = value; OnPropertyChanged(); } }
  public List<GenreM> Genres { get; set; }
  public List<KeywordM> Keywords { get; set; }
  public ObservableCollection<DateOnly> Seen { get; set; } = [];
  public string MPAA { get; set; }
  public string Plot { get; set; }
  public MediaItemM Poster { get => _poster; set { _poster = value; OnPropertyChanged(); } }
  public List<MediaItemM> MediaItems { get; set; }
  public MovieDetailIdM DetailId { get; set; }

  public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
  public KeywordM[] DisplayKeywords => GetDisplayKeywords();

  public string FormatedLength =>
    TimeSpan.FromMinutes(Length).ToString(@"h\:mm");

  public MovieM(int id, string title) {
    Id = id;
    Title = title;
  }

  public override int GetHashCode() => Id;

  private KeywordM[] GetDisplayKeywords() =>
    Keywords?
      .EmptyIfNull()
      .SelectMany(x => x.GetThisAndParents())
      .Distinct()
      .OrderBy(x => x.GetFullName("/", k => k.Name))
      .ToArray();
}