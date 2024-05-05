using MH.Utils;
using MovieManager.Common.Models;
using PictureManager.Interfaces.Models;
using PictureManager.Interfaces.Repositories;
using System;
using System.IO;
using System.Linq;

namespace MovieManager.Common.Repositories;

public sealed class CoreR : SimpleDB {
  private readonly ICoreR _phCoreR;

  public ActorR Actor { get; }
  public ActorDetailIdR ActorDetailId { get; }
  public GenreR Genre { get; }
  public CharacterR Character { get; }
  public MovieDetailIdR MovieDetailId { get; }
  public MovieR Movie { get; }

  public string ActorsDir { get; }
  public string ImagesDir { get; }
  public string PostersDir { get; }
  public IFolderM ActorsFolder { get; set; }
  public IFolderM ImagesFolder { get; set; }
  public IFolderM PostersFolder { get; set; }
  public IFolderM RootFolder { get; set; }

  public CoreR(ICoreR phCoreR, Core core) : base(Path.Combine(core.BaseDir, "db")) {
    _phCoreR = phCoreR;
    ActorsDir = Path.Combine(core.BaseDir, "actors");
    ImagesDir = Path.Combine(core.BaseDir, "images");
    PostersDir = Path.Combine(core.BaseDir, "posters");
    RootFolder = GetFolder(core.BaseDir);

    Actor = new(this, phCoreR);
    ActorDetailId = new(this);
    Genre = new(this);
    Character = new(this, phCoreR);
    MovieDetailId = new(this);
    Movie = new(this, phCoreR);
  }

  public void AddDataAdapters() {
    AddTableDataAdapter(Actor);
    AddTableDataAdapter(ActorDetailId);
    AddTableDataAdapter(Genre);
    AddTableDataAdapter(Character);
    AddTableDataAdapter(MovieDetailId);
    AddTableDataAdapter(Movie);
  }

  public void AttachEvents() {
    Movie.ItemDeletedEvent += OnMovieDeleted;
  }

  private void OnMovieDeleted(object sender, MovieM e) {
    Character.ItemsDelete(Character.All.Where(x => ReferenceEquals(x.Movie, e)).ToList());
    MovieDetailId.ItemDelete(MovieDetailId.All.Single(x => ReferenceEquals(x.Movie, e)));
  }

  public void SetActorsFolder() {
    if (ActorsFolder != null) return;
    ActorsFolder = GetFolder(ActorsDir);
  }

  public void SetImagesFolder() {
    if (ImagesFolder != null) return;
    ImagesFolder = GetFolder(ImagesDir);
  }

  public void SetPostersFolder() {
    if (PostersFolder != null) return;
    PostersFolder = GetFolder(PostersDir);
  }

  public IFolderM GetFolder(string path) {
    if (Directory.Exists(path))
      return _phCoreR.Folder.GetFolder(path);

    try {
      Directory.CreateDirectory(path!);
      return _phCoreR.Folder.GetFolder(path);
    }
    catch (Exception ex) {
      Log.Error(ex);
      return null;
    }
  }
}