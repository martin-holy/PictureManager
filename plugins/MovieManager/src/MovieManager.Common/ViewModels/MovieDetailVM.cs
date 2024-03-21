using MH.Utils.BaseClasses;
using MovieManager.Common.Models;

namespace MovieManager.Common.ViewModels;

public sealed class MovieDetailVM : ObservableObject {
  private MovieM _movieM;

  public MovieM MovieM { get => _movieM; set { _movieM = value; OnPropertyChanged(); } }

  public void Reload(MovieM movie) {
    MovieM = movie;
  }
}