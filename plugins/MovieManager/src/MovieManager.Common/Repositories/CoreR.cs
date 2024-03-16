using MH.Utils;
using PictureManager.Plugins.Common.Interfaces.Repositories;

namespace MovieManager.Common.Repositories;

public sealed  class CoreR : SimpleDB {
  public GenreR Genre { get; }
  public MovieR Movie { get; }

  public CoreR(IPluginCoreR pmCoreR) {
    Genre = new();
    Movie = new(this, pmCoreR);
  }

  public void AddDataAdapters() {
    AddTableDataAdapter(Genre);
    AddTableDataAdapter(Movie);
  }
}