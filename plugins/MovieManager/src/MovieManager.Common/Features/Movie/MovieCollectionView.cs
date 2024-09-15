using MH.UI.Controls;
using MH.Utils.BaseClasses;
using PictureManager.Common.Features.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using PM = PictureManager.Common;

namespace MovieManager.Common.Features.Movie;

public class MovieCollectionView() : CollectionView<MovieM>(MH.UI.Res.IconMovieClapper, "Movies", ViewMode.ThumbBig) {
  public override int GetItemSize(ViewMode viewMode, MovieM item, bool getWidth) {
    var scale = PM.Core.Settings.MediaItem.MediaItemThumbScale;

    if (item.Poster != null)
      return (int)((getWidth ? item.Poster.ThumbWidth : item.Poster.ThumbHeight) * scale);

    var h = PM.Core.Settings.MediaItem.ThumbSize * scale;
    var w = h / 1.5;

    return getWidth ? (int)w : (int)h;
  }

  public override IEnumerable<GroupByItem<MovieM>> GetGroupByItems(IEnumerable<MovieM> source) {
    var src = source.ToArray();
    var top = new List<GroupByItem<MovieM>> { GroupByItems.GetKeywordsInGroup(src) };

    return top;
  }

  public override int SortCompare(MovieM itemA, MovieM itemB) =>
    string.Compare(itemA.Title, itemB.Title, StringComparison.CurrentCultureIgnoreCase);

  public override void OnItemSelected(SelectionEventArgs<MovieM> e) =>
    Core.S.Movie.Select(e.Items, e.Item, e.IsCtrlOn, e.IsShiftOn);
}