using System.Collections.Generic;
using System.Linq;
using MH.UI;
using MH.UI.Controls;
using MH.Utils.BaseClasses;
using MovieManager.Common.CollectionViews;
using MovieManager.Common.Models;

namespace MovieManager.Common.ViewModels;

public sealed class MoviesVM : CollectionViewMovies {
  

  public MoviesVM() {
    
  }

  public void Open(IEnumerable<MovieM> items) {
    var source = items.OrderBy(x => x.Title).ToList();
    Reload(source, GroupMode.ThenByRecursive, null, true);
  }
}