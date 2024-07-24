using MH.Utils;
using MH.Utils.Extensions;
using MovieManager.Common.Features.Actor;
using MovieManager.Common.Features.Movie;
using MovieManager.Plugins.Common.DTOs;
using PictureManager.Common.Features.Folder;
using PictureManager.Common.Features.MediaItem;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MovieManager.Common.Features.Import;

public class ImportS {
  private readonly CoreR _coreR;
  private readonly CoreS _coreS;

  public event EventHandler<MovieM> MovieImportedEvent = delegate { };

  public ImportS(CoreR coreR, CoreS coreS) {
    _coreR = coreR;
    _coreS = coreS;
  }

  public async Task ImportMovie(SearchResult? result, IProgress<string> progress, CancellationToken token) {
    if (result == null) return;
    progress.Report($"Importing '{result.Name}' ...", true);

    var movie = _coreR.MovieDetailId.GetMovie(result.DetailId);
    if (movie != null) {
      progress.Report("The movie is already in the DB.", true);
      return;
    }

    var md = await Core.Inst.ImportPlugin!.GetMovieDetail(result.DetailId);
    if (md == null) {
      progress.Report("Information about the movie not found.", true);
      return;
    }

    if (token.IsCancellationRequested) return;
    movie = _coreR.Movie.ItemCreate(md);
    movie.DetailId = _coreR.MovieDetailId.ItemCreate(md.DetailId, movie);
    await DownloadMoviePoster(progress, md, movie);
    await ImportActors(progress, md, movie, token);
    //await ImportImages(progress, md, movie);
    progress.Report($"Importing '{movie.Title}' completed.", true);
    MovieImportedEvent(this, movie);
  }

  private async Task ImportActors(IProgress<string> progress, MovieDetail md, MovieM movie, CancellationToken token) {
    progress.Report($"Importing {md.Cast.Length} actors ...", true);

    foreach (var cast in md.Cast) {
      if (token.IsCancellationRequested) break;
      progress.Report($"{cast.Actor.Name} ({string.Join(", ", cast.Characters)})", true);
      var actor = _coreR.ActorDetailId.GetActor(cast.Actor.DetailId);

      if (actor == null) {
        actor = _coreR.Actor.ItemCreate(cast.Actor.Name);
        actor.DetailId = _coreR.ActorDetailId.ItemCreate(cast.Actor.DetailId, actor);
      }

      if (cast.Actor.Image != null)
        await DownloadActorImage(actor, cast.Actor.Image);

      foreach (var character in cast.Characters)
        _coreR.Character.ItemCreate(character, actor, movie);
    }
  }

  // Images are disabled because of poor content. Random screenshots or not from movie itself.
  /*private async Task ImportImages(IProgress<string> progress, MovieDetail md, MovieM movie) {
    if (md.Images == null || md.Images.Length == 0 || GetMovieImagesFolder(movie) is not { } imgFolder) return;

    progress.Report($"Downloading {md.Images.Length} images ...", true);
    movie.MediaItems ??= [];

    for (var i = 0; i < md.Images.Length; i++) {
      var di = md.Images[i];
      var imgId = string.IsNullOrEmpty(di.Id) ? i.ToString() : di.Id;
      progress.Report(di.Desc, true);
      var img = await DownloadImage(di, imgFolder, $"{md.Year} {imgId} {di.Desc}.jpg");
      movie.MediaItems.Add(img);
    }
  }*/

  private async Task DownloadActorImage(ActorM actor, Image image) {
    if (actor.Image != null) return;
    var fileName = GetActorImageFileName(actor);
    actor.Image = await DownloadImage(image, _coreR.ActorsFolder, fileName);
  }

  private async Task DownloadMoviePoster(IProgress<string> progress, MovieDetail md, MovieM movie) {
    if (md.Poster == null) return;
    progress.Report("Downloading the movie poster ...", true);
    var fileName = GetMoviePosterFileName(movie);
    movie.Poster = await DownloadImage(md.Poster, _coreR.PostersFolder, fileName);

    if (movie.Poster != null) {
      movie.MediaItems ??= [];
      movie.MediaItems.Add(movie.Poster);
    }
  }

  private async Task<MediaItemM?> DownloadImage(Image? image, FolderM? folder, string fileName) {
    if (image == null || folder == null || string.IsNullOrEmpty(image.Url)) return null;
    var filePath = Path.Combine(folder.FullPath, fileName);
    try {
      if (!Path.Exists(filePath))
        await Plugins.Common.Core.DownloadAndSaveFile(image.Url, filePath);

      return _coreS.PMCoreS.MediaItem.GetMediaItem(folder, fileName);
    }
    catch (Exception ex) {
      Log.Error(ex);
      return null;
    }
  }

  private static string GetActorImageFileName(ActorM actor) =>
    $"{actor.DetailId.DetailName}-{actor.DetailId.DetailId}-{IOExtensions.CleanFileName(actor.Name)}.jpg";

  private static string GetMoviePosterFileName(MovieM movie) =>
    $"{movie.Year}-{movie.DetailId.DetailName}-{movie.DetailId.DetailId}-{IOExtensions.CleanFileName(movie.Title)}.jpg";

  /*private IFolderM GetMovieImagesFolder(MovieM movie) =>
    _coreR.GetFolder(Path.Combine(_coreR.ImagesFolder.FullPath, $"{movie.Year} {movie.Title}"));*/
}