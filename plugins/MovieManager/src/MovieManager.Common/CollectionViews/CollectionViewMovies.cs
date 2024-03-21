using MH.UI.Controls;
using MH.Utils.BaseClasses;
using MovieManager.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MovieManager.Common.CollectionViews;

public class CollectionViewMovies : CollectionView<MovieM> {
  public CollectionViewMovies() {
    Icon = "IconMovieClapper";
    Name = "Movies";
  }

  public override int GetItemSize(MovieM item, bool getWidth) =>
    getWidth ? 200 : 300;

  public override IEnumerable<GroupByItem<MovieM>> GetGroupByItems(IEnumerable<MovieM> source) =>
    Enumerable.Empty<GroupByItem<MovieM>>();

  public override int SortCompare(MovieM itemA, MovieM itemB) =>
    string.Compare(itemA.Title, itemB.Title, StringComparison.CurrentCultureIgnoreCase);

  public override void OnItemSelected(SelectionEventArgs<MovieM> e) =>
    Core.S.Movie.Select(e.Items, e.Item, e.IsCtrlOn, e.IsShiftOn);
}