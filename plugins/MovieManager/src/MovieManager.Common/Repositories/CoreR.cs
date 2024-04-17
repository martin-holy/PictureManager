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

  public string PostersDir { get; }
  public IFolderM PostersFolder { get; set; }

  public CoreR(ICoreR phCoreR, Core core) : base(Path.Combine(core.BaseDir, "db")) {
    _phCoreR = phCoreR;
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

  public void SetPosterFolder() {
    if (PostersFolder != null) return;

    if (!Directory.Exists(PostersDir)) {
      try {
        Directory.CreateDirectory(PostersDir);
      }
      catch (Exception ex) {
        Log.Error(ex);
        return;
      }
    }

    PostersFolder = _phCoreR.Folder.GetFolder(PostersDir);
  }
}