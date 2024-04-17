using MH.Utils;
using PictureManager.Interfaces.Repositories;
using System.IO;

namespace MovieManager.Common.Repositories;

public sealed  class CoreR : SimpleDB {
  public ActorR Actor { get; }
  public ActorDetailIdR ActorDetailId { get; }
  public GenreR Genre { get; }
  public CharacterR Character { get; }
  public MovieDetailIdR MovieDetailId { get; }
  public MovieR Movie { get; }

  public CoreR(ICoreR phCoreR) : base(Path.Combine("plugins", "MovieManager", "db")) {
    Actor = new(this, phCoreR);
    ActorDetailId = new(this);
    Genre = new(this);
    Character = new(this);
    MovieDetailId = new(this);
    Movie = new(this);
  }

  public void AddDataAdapters() {
    AddTableDataAdapter(Actor);
    AddTableDataAdapter(ActorDetailId);
    AddTableDataAdapter(Genre);
    AddTableDataAdapter(Character);
    AddTableDataAdapter(MovieDetailId);
    AddTableDataAdapter(Movie);
  }
}