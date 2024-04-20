using System;
using MH.Utils;
using PictureManager.Interfaces.Models;
using PictureManager.Interfaces.Repositories;
using System.IO;

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
  public string PostersDir { get; }
  public IFolderM ActorsFolder { get; set; }
  public IFolderM PostersFolder { get; set; }

  public CoreR(ICoreR phCoreR, Core core) : base(Path.Combine(core.BaseDir, "db")) {
    _phCoreR = phCoreR;
    ActorsDir = Path.Combine(core.BaseDir, "actors");
    PostersDir = Path.Combine(core.BaseDir, "posters");

    Actor = new(this, phCoreR);
    ActorDetailId = new(this);
    Genre = new(this);
    Character = new(this);
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

  public void SetActorsFolder() {
    if (ActorsFolder != null) return;
    ActorsFolder = GetFolder(ActorsDir);
  }

  public void SetPostersFolder() {
    if (PostersFolder != null) return;
    PostersFolder = GetFolder(PostersDir);
  }

  private IFolderM GetFolder(string path) {
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