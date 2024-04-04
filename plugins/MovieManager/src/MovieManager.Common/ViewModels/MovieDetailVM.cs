using MH.Utils.BaseClasses;
using MovieManager.Common.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace MovieManager.Common.ViewModels;

public sealed class MovieDetailVM : ObservableObject {
  private MovieM _movieM;

  public MovieM MovieM { get => _movieM; set { _movieM = value; OnPropertyChanged(); } }
  public ObservableCollection<ListItem> Casts { get; } = [];

  public void Reload(MovieM movie) {
    MovieM = movie;
    Casts.Clear();

    foreach (var character in Core.R.Character.All.Where(x => ReferenceEquals(x.Movie, movie)))
      Casts.Add(new(null, $"{character.Actor.Name} ({character.Name})"));
  }
}