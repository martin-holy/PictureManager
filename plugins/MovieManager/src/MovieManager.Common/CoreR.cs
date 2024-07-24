using MH.Utils;
using MovieManager.Common.Features.Actor;
using MovieManager.Common.Features.Character;
using MovieManager.Common.Features.Genre;
using MovieManager.Common.Features.Movie;
using PictureManager.Common.Features.Folder;
using PictureManager.Common.Features.Keyword;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.Person;
using PictureManager.Common.Features.Segment;
using System;
using System.IO;
using System.Linq;
using PM = PictureManager.Common;

namespace MovieManager.Common;

public sealed class CoreR : SimpleDB {
  public readonly PM.CoreR PMCoreR;
  private readonly Core _core;

  public ActorR Actor { get; }
  public ActorDetailIdR ActorDetailId { get; }
  public GenreR Genre { get; }
  public CharacterR Character { get; }
  public MovieDetailIdR MovieDetailId { get; }
  public MovieR Movie { get; }

  public FolderM? ActorsFolder { get; set; }
  //public FolderM ImagesFolder { get; set; }
  public FolderM? PostersFolder { get; set; }
  public FolderM? RootFolder { get; set; }

  public CoreR(PM.CoreR pmCoreR, Core core) : base(Path.Combine(core.BaseDir, "db")) {
    PMCoreR = pmCoreR;
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
    PMCoreR.Keyword.ItemDeletedEvent += OnKeywordDeleted;
    PMCoreR.MediaItem.ItemDeletedEvent += OnMediaItemDeleted;
    PMCoreR.Person.ItemDeletedEvent += OnPersonDeleted;
    PMCoreR.Segment.ItemDeletedEvent += OnSegmentDeleted;
  }

  private void OnMovieDeleted(object? sender, MovieM e) {
    Character.ItemsDelete(Character.All.Where(x => ReferenceEquals(x.Movie, e)).ToList());
    MovieDetailId.ItemDelete(MovieDetailId.All.Single(x => ReferenceEquals(x.Movie, e)));
  }

  private void OnKeywordDeleted(object? sender, KeywordM e) {
    Movie.OnKeywordDeleted(e);
  }

  private void OnMediaItemDeleted(object? sender, MediaItemM e) {
    Actor.OnMediaItemDeleted(e);
    Movie.OnMediaItemDeleted(e);
  }

  private void OnPersonDeleted(object? sender, PersonM e) {
    Actor.OnPersonDeleted(e);
  }

  private void OnSegmentDeleted(object? sender, SegmentM e) {
    Character.OnSegmentDeleted(e);
  }

  public void SetFolders() {
    ActorsFolder = GetFolder(Path.Combine(_core.BaseDir, "actors"));
    //ImagesFolder = GetFolder(Path.Combine(_core.BaseDir, "images"));
    PostersFolder = GetFolder(Path.Combine(_core.BaseDir, "posters"));
    RootFolder = GetFolder(_core.BaseDir);
  }

  public FolderM? GetFolder(string path) {
    if (Directory.Exists(path))
      return PMCoreR.Folder.GetFolder(path);

    try {
      Directory.CreateDirectory(path);
      return PMCoreR.Folder.GetFolder(path);
    }
    catch (Exception ex) {
      Log.Error(ex);
      return null;
    }
  }
}