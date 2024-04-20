using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MovieManager.Common.Models;
using MovieManager.Common.Services;
using MovieManager.Plugins.Common.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace MovieManager.Common.ViewModels;

public class ImportVM : ObservableObject {
  private readonly List<string> _searchMoviesQueue = [];

  public ObservableCollection<SearchResult> MovieSearchResults { get; } = [];

  public AsyncRelayCommand<string> ImportMoviesCommand { get; }
  public AsyncRelayCommand<SearchResult> ResolveSearchMovieResultCommand { get; }
  public AsyncRelayCommand TestCommand { get; }

  public ImportVM(ImportS importS) {
    ImportMoviesCommand = new(ImportMovies, "IconBug", "Import");
    ResolveSearchMovieResultCommand = new(ResolveSearchMovieResult);
    TestCommand = new(Test);
  }

  private async Task Test() {
    var movieDetail = await Core.MovieDetail.GetMovieDetail(null);
  }

  private Task ImportMovies(string titles) {
    _searchMoviesQueue.Clear();
    _searchMoviesQueue.AddRange(titles.Split(
      new[] { Environment.NewLine },
      StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
    ));

    Core.R.SetActorsFolder();
    Core.R.SetPostersFolder();
    return SearchMovie();
  }

  private async Task SearchMovie() {
    if (_searchMoviesQueue.Pop() is not { } title) return;
    var results = await Core.MovieSearch.SearchMovie(title);

    switch (results.Length) {
      case 0:
        await SearchMovie();
        // TODO notify nothing was found
        break;
      case 1:
        await ImportMovie(results[0]);
        await SearchMovie();
        break;
      default:
        ResolveSearchMovieResults(results);
        break;
    }
  }

  private void ResolveSearchMovieResults(SearchResult[] results) {
    foreach (var result in results)
      MovieSearchResults.Add(result);
  }

  private async Task ResolveSearchMovieResult(SearchResult result) {
    MovieSearchResults.Clear();
    await ImportMovie(result);
    await SearchMovie();
  }

  private async Task ImportMovie(SearchResult result) {
    var movie = Core.R.MovieDetailId.GetMovie(result.DetailId);
    if (movie != null) return; // TODO notify that movie is already in DB

    var movieDetail = await Core.MovieDetail.GetMovieDetail(result.DetailId);
    if (movieDetail == null) return;
    movie = Core.R.Movie.ItemCreate(movieDetail);
    movie.DetailId = Core.R.MovieDetailId.ItemCreate(movieDetail.DetailId, movie);

    foreach (var cast in movieDetail.Cast) {
      var actor = Core.R.ActorDetailId.GetActor(cast.Actor.DetailId);

      if (actor == null) {
        actor = Core.R.Actor.ItemCreate(cast.Actor.Name);
        Core.R.ActorDetailId.ItemCreate(cast.Actor.DetailId, actor);
      }
      
      foreach (var character in cast.Characters)
        Core.R.Character.ItemCreate(character, actor, movie);
    }

    ImportPoster(movieDetail.Poster, movie);
  }

  private void ImportPoster(Image poster, MovieM movie) {
    if (poster == null || Core.R.PostersFolder == null) return;

    var filePath = Path.Combine(Core.R.PostersDir, movie.PosterFileName);

    Tasks.DoWork(
      () => Plugins.Common.Core.DownloadAndSaveFile(poster.Url, filePath),
      _ => movie.Poster = Core.S.PhCoreS.MediaItem.ImportMediaItem(Core.R.PostersFolder, movie.PosterFileName),
      Log.Error);
  }

  public void Open() {

  }
}