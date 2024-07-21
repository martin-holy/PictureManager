using MH.Utils.BaseClasses;

namespace MovieManager.Common.Features.Genre;

public sealed class GenreFilterVM : ObservableObject {
  private bool _unset = true;
  private bool _and;
  private bool _or;
  private bool _not;

  public GenreM Genre { get; set; }
  public bool Unset { get => _unset; set { _unset = value; OnPropertyChanged(); } }
  public bool And { get => _and; set { _and = value; OnPropertyChanged(); } }
  public bool Or { get => _or; set { _or = value; OnPropertyChanged(); } }
  public bool Not { get => _not; set { _not = value; OnPropertyChanged(); } }

  public GenreFilterVM(GenreM genre) {
    Genre = genre;
  }

  public void Reset() {
    And = false;
    Or = false;
    Not = false;
    Unset = true;
  }
}