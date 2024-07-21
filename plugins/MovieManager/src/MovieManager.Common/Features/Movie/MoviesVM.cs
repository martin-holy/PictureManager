using MH.UI.Controls;
using System.Collections.Generic;
using System.Linq;

namespace MovieManager.Common.Features.Movie;

public sealed class MoviesVM : CollectionViewMovies {
  public void Open(IEnumerable<MovieM> items) {
    var source = items.OrderBy(x => x.Title).ToList();

    foreach (var movie in source.Where(x => x.Poster != null))
      movie.Poster!.SetThumbSize();

    Reload(source, GroupMode.ThenByRecursive, null, true);
  }
}