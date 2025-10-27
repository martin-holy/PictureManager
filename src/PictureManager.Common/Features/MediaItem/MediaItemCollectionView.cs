using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.EventsArgs;
using PictureManager.Common.Features.Common;
using PictureManager.Common.Features.Person;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Common.Features.MediaItem;

public class MediaItemCollectionView : CollectionView<MediaItemM> {
  private double _thumbScale;
  private static readonly IReadOnlyList<SortField<MediaItemM>> _sortFields = [
    new SortField<MediaItemM>("File name", x => x.FileName, StringComparer.CurrentCultureIgnoreCase)
  ];

  public double ThumbScale { get => _thumbScale; set { _thumbScale = value; OnPropertyChanged(); } }
  public Selecting<MediaItemM> Selected { get; } = new();

  public RelayCommand ThumbScaleChangedCommand { get; }

  public MediaItemCollectionView(double thumbScale) : base(Res.IconImageMultiple, "Media Items", [ViewMode.ThumbBig]) {
    ThumbScale = thumbScale;
    ThumbScaleChangedCommand = new(_onThumbScaleChanged);
    DefaultSortField = _sortFields[0];
  }

  private void _onThumbScaleChanged() =>
    ReWrapAll();

  public override IEnumerable<GroupByItem<MediaItemM>> GetGroupByItems(IEnumerable<MediaItemM> source) {
    var src = source.ToArray();
    var top = new List<GroupByItem<MediaItemM>>();
    // TODO remove trunk from folders => remove common branch starting from root
    top.AddRange(GroupByItems.GetFolders(src));
    top.Add(GroupByItems.GetDatesInGroup(src));
    top.AddRange(GroupByItems.GetGeoNames(src));
    top.Add(GroupByItems.GetKeywordsInGroup(src));
    top.Add(GroupByItems.GetPeopleInGroup(src));

    return top;
  }

  public override int GetItemSize(ViewMode viewMode, MediaItemM item, bool getWidth) =>
    (int)((getWidth ? item.ThumbWidth : item.ThumbHeight) * ThumbScale);

  public override IEnumerable<SortField<MediaItemM>> GetSortFields() => _sortFields;

  public override int SortCompare(MediaItemM itemA, MediaItemM itemB) =>
    string.Compare(itemA.FileName, itemB.FileName, StringComparison.CurrentCultureIgnoreCase);

  protected override void _onItemSelected(SelectionEventArgs<MediaItemM> e) =>
    Selected.Select(e.Items, e.Item, e.IsCtrlOn, e.IsShiftOn);

  public override string GetItemTemplateName(ViewMode viewMode) => "PM.DT.MediaItem.Thumb-Full";
}