using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MovieManager.Common.Services;
using MovieManager.Plugins.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MovieManager.Common.ViewModels;

public class ImportVM : ObservableObject {
  private readonly List<string> _searchMoviesQueue = [];

  public ObservableCollection<IMovieSearchResult> MovieSearchResults { get; } = [];

  public RelayCommand<string> ImportMoviesCommand { get; }
  public RelayCommand<IMovieSearchResult> ResolveSearchMovieResultCommand { get; }

  public ImportVM(ImportS importS) {
    ImportMoviesCommand = new(ImportMovies, "IconBug", "Import");
    ResolveSearchMovieResultCommand = new(ResolveSearchMovieResult);
  }

  private void ImportMovies(string titles) {
    _searchMoviesQueue.Clear();
    _searchMoviesQueue.AddRange(titles.Split(
      new[] { Environment.NewLine },
      StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
    ));

    SearchMovie();
  }

  private void SearchMovie() {
    if (_searchMoviesQueue.Pop() is not { } title) return;
    var results = Core.MovieSearch.SearchMovie(title);

    switch (results.Length) {
      case 0:
        SearchMovie();
        // TODO notify nothing was found
        break;
      case 1:
        ImportMovie(results[0]);
        SearchMovie();
        break;
      default:
        ResolveSearchMovieResults(results);
        break;
    }
  }

  private void ResolveSearchMovieResults(IMovieSearchResult[] results) {
    foreach (var result in results)
      MovieSearchResults.Add(result);
  }

  private void ResolveSearchMovieResult(IMovieSearchResult result) {
    MovieSearchResults.Clear();
    ImportMovie(result);
    SearchMovie();
  }

  private void ImportMovie(IMovieSearchResult result) {
    // TODO do not allow duplicities
    var movieDetail = Core.MovieDetail.GetMovieDetail(result.DetailId);
    var movie = Core.R.Movie.ItemCreate(movieDetail);

    foreach (var cast in movieDetail.Casts) {
      var actor = Core.R.ActorDetailId.GetActor(cast.DetailId);

      if (actor == null) {
        actor = Core.R.Actor.ItemCreate(cast.Name);
        Core.R.ActorDetailId.ItemCreate(cast.DetailId, actor);
      }
      
      foreach (var character in cast.Characters)
        Core.R.Character.ItemCreate(character, actor, movie);
    }

    // TODO DetailId relation
  }

  public void Open() {

  }
}