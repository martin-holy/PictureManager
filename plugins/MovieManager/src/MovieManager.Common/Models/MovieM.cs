using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Interfaces.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MovieManager.Common.Models;

public sealed class MovieM : ObservableObject, ISelectable {
  private bool _isSelected;
  private double _myRating;

  public int Id { get; }
  public string Title { get; set; }
  public int Year { get; set; }
  public int? YearEnd { get; set; }
  public int Length { get; set; }
  public double Rating { get; set; }
  public double MyRating { get => _myRating; set { _myRating = value; OnPropertyChanged(); } }
  public List<GenreM> Genres { get; set; }
  public List<IKeywordM> Keywords { get; set; }
  public ObservableCollection<DateOnly> Seen { get; set; } = [];
  public string MPAA { get; set; }
  public string Plot { get; set; }
  public IMediaItemM Poster { get; set; }
  public List<IMediaItemM> MediaItems { get; set; }
  public MovieDetailIdM DetailId { get; set; }

  public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
  public IKeywordM[] DisplayKeywords => GetDisplayKeywords();

  public string FormatedLength =>
    TimeSpan.FromMinutes(Length).ToString(@"h\:mm");

  public MovieM(int id, string title) {
    Id = id;
    Title = title;
  }

  public override int GetHashCode() => Id;

  private IKeywordM[] GetDisplayKeywords() =>
    Keywords?
      .EmptyIfNull()
      .SelectMany(x => x.GetThisAndParents())
      .Distinct()
      .OrderBy(x => x.GetFullName("/", k => k.Name))
      .ToArray();
}