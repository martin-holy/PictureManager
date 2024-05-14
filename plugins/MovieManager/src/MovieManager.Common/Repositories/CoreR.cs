using MH.Utils;
using MovieManager.Common.Models;
using PictureManager.Interfaces.Models;
using PictureManager.Interfaces.Repositories;
using System;
using System.IO;
using System.Linq;

namespace MovieManager.Common.Repositories;

public sealed class CoreR : SimpleDB {
  private readonly IPMCoreR _pmCoreR;
  private readonly Core _core;

  public ActorR Actor { get; }
  public ActorDetailIdR ActorDetailId { get; }
  public GenreR Genre { get; }
  public CharacterR Character { get; }
  public MovieDetailIdR MovieDetailId { get; }
  public MovieR Movie { get; }

  public IFolderM ActorsFolder { get; set; }
  public IFolderM ImagesFolder { get; set; }
  public IFolderM PostersFolder { get; set; }
  public IFolderM RootFolder { get; set; }

  public CoreR(IPMCoreR pmCoreR, Core core) : base(Path.Combine(core.BaseDir, "db")) {
    _pmCoreR = pmCoreR;
    _core = core;

    Actor = new(this, pmCoreR);
    ActorDetailId = new(this);
    Genre = new(this);
    Character = new(this, pmCoreR);
    MovieDetailId = new(this);
    Movie = new(this, pmCoreR);
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
    _pmCoreR.MediaItem.ItemDeletedEvent += OnMediaItemDeleted;
    _pmCoreR.Person.ItemDeletedEvent += OnPersonDeleted;
    _pmCoreR.Segment.ItemDeletedEvent += OnSegmentDeleted;
  }

  private void OnMovieDeleted(object sender, MovieM e) {
    Character.ItemsDelete(Character.All.Where(x => ReferenceEquals(x.Movie, e)).ToList());
    MovieDetailId.ItemDelete(MovieDetailId.All.Single(x => ReferenceEquals(x.Movie, e)));
  }

  private void OnMediaItemDeleted(object sender, IMediaItemM e) {
    Actor.OnMediaItemDeleted(e);
    Movie.OnMediaItemDeleted(e);
  }

  private void OnPersonDeleted(object sender, IPersonM e) {
    Actor.OnPersonDeleted(e);
  }

  private void OnSegmentDeleted(object sender, ISegmentM e) {
    Character.OnSegmentDeleted(e);
  }

  public void SetFolders() {
    ActorsFolder = GetFolder(Path.Combine(_core.BaseDir, "actors"));
    ImagesFolder = GetFolder(Path.Combine(_core.BaseDir, "images"));
    PostersFolder = GetFolder(Path.Combine(_core.BaseDir, "posters"));
    RootFolder = GetFolder(_core.BaseDir);
  }

  public IFolderM GetFolder(string path) {
    if (Directory.Exists(path))
      return _pmCoreR.Folder.GetFolder(path);

    try {
      Directory.CreateDirectory(path!);
      return _pmCoreR.Folder.GetFolder(path);
    }
    catch (Exception ex) {
      Log.Error(ex);
      return null;
    }
  }
}