using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;

namespace MovieManager.Common.Features.Genre;

public sealed class GenreM : ObservableObject, IHaveName {
  private string _name;

  public int Id { get; }
  public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }

  public GenreM(int id, string name) {
    Id = id;
    _name = name;
  }

  public override int GetHashCode() => Id;
}