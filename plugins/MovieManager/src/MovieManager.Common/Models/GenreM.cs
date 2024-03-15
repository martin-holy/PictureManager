using MH.Utils.BaseClasses;

namespace MovieManager.Common.Models;

public sealed class GenreM : ObservableObject {
  private string _name;
  
  public int Id { get; }
  public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }

  public GenreM(int id, string name) {
    Id = id;
    Name = name;
  }

  public override int GetHashCode() => Id;
}