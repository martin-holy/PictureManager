using MH.Utils;

namespace MovieManager.Common.Repositories;

public sealed  class CoreR : SimpleDB {
  public GenreR Genre { get; }
  public MovieR Movie { get; }

  public CoreR() {
    Genre = new();
    Movie = new(this);
  }

  public void AddDataAdapters() {

  }
}