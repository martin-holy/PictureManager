using MH.Utils;
using MH.Utils.Extensions;
using MovieManager.Common.Models;
using MovieManager.Common.Repositories;
using MovieManager.Plugins.Common.Models;
using PictureManager.Interfaces.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MovieManager.Common.Services;

public class ImportS {
  private readonly CoreR _coreR;
  private readonly CoreS _coreS;

  public ImportS(CoreR coreR, CoreS coreS) {
    _coreR = coreR;
    _coreS = coreS;
  }

  public void PrepareForImport() {
    _coreR.SetActorsFolder();
    _coreR.SetPostersFolder();
  }

  public async Task ImportMovie(SearchResult result, IProgress<string> progress) {
    if (result == null) return;
    progress.Report($"Importing '{result.Name}' ...", true);

    var movie = _coreR.MovieDetailId.GetMovie(result.DetailId);
    if (movie != null) {
      progress.Report("The movie is already in the DB.", true);
      return;
    }

    var movieDetail = await Core.MovieDetail.GetMovieDetail(result.DetailId);
    if (movieDetail == null) {
      progress.Report("Information about the movie not found.", true);
      return;
    }

    movie = _coreR.Movie.ItemCreate(movieDetail);
    movie.DetailId = _coreR.MovieDetailId.ItemCreate(movieDetail.DetailId, movie);

    progress.Report("Downloading the movie poster ...", true);
    await DownloadMoviePoster(movie, movieDetail.Poster);

    progress.Report($"Importing {movieDetail.Cast.Length} actors ...", true);
    foreach (var cast in movieDetail.Cast) {
      progress.Report($"{cast.Actor.Name} ({string.Join(", ", cast.Characters)})", true);
      var actor = _coreR.ActorDetailId.GetActor(cast.Actor.DetailId);

      if (actor == null) {
        actor = _coreR.Actor.ItemCreate(cast.Actor.Name);
        actor.DetailId = _coreR.ActorDetailId.ItemCreate(cast.Actor.DetailId, actor);
      }

      await DownloadActorImage(actor, cast.Actor.Image);
      
      foreach (var character in cast.Characters)
        _coreR.Character.ItemCreate(character, actor, movie);
    }

    progress.Report($"Importing '{movie.Title}' completed.", true);
  }

  private async Task DownloadActorImage(ActorM actor, Image image) {
    if (actor.Image != null) return;
    var fileName = GetActorImageFileName(actor);
    actor.Image = await DownloadImage(image, _coreR.ActorsFolder, fileName);
  }

  private async Task DownloadMoviePoster(MovieM movie, Image image) {
    var fileName = GetMoviePosterFileName(movie);
    movie.Poster = await DownloadImage(image, _coreR.PostersFolder, fileName);
  }

  private async Task<IMediaItemM> DownloadImage(Image image, IFolderM folder, string fileName) {
    if (image == null || string.IsNullOrEmpty(image.Url)) return null;
    var filePath = Path.Combine(folder.FullPath, fileName);
    try {
      if (!Path.Exists(filePath)) 
        await Plugins.Common.Core.DownloadAndSaveFile(image.Url, filePath);

      return _coreS.PhCoreS.MediaItem.GetMediaItem(folder, fileName);
    }
    catch (Exception ex) {
      Log.Error(ex);
      return null;
    }
  }

  private static string GetActorImageFileName(ActorM actor) =>
    $"{actor.DetailId.DetailName}-{actor.DetailId.DetailId}-{actor.Name}.jpg";

  private static string GetMoviePosterFileName(MovieM movie) =>
    $"{movie.Year}-{movie.DetailId.DetailName}-{movie.DetailId.DetailId}-{movie.Title}.jpg";
}