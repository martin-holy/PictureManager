using MH.UI.Controls;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Common.Features.GeoName;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.WhatIsNew;
using System.Collections.Specialized;

namespace PictureManager.Common.Layout;

public class MainMenuVM : TreeView {
  private readonly MenuItem _leftTabs = new(Res.IconTabLeft, "Left tabs");
  private readonly MenuItem _middleTabs = new(Res.IconTabMiddle, "Middle tabs");
  private readonly MenuItem _rightTabs = new(Res.IconTabRight, "Right tabs");

  public string Icon { get; } = Res.IconThreeBars;

  public void Build(CoreVM coreVM) {
    RootHolder.Clear();

    var geoLocation = new MenuItem(Res.IconLocationCheckin, "GeoLocation");
    geoLocation.Add(new(CoreVM.GetGeoNamesFromWebCommand));
    geoLocation.Add(new(GeoNameVM.NewGeoNameFromGpsCommand));
    geoLocation.Add(new(CoreVM.ReadGeoLocationFromFilesCommand));

    var mediaItem = new MenuItem(Res.IconImageMultiple, "Media Items");
    mediaItem.Add(new(MediaItemVM.CommentCommand) { InputGestureText = "Ctrl+K"});
    mediaItem.Add(new(CoreVM.CompressImagesCommand));
    mediaItem.Add(new(MediaItemsViewsVM.CopyPathsCommand));
    mediaItem.Add(new(MediaItemVM.DeleteCommand));
    mediaItem.Add(new(CoreVM.ImagesToVideoCommand));
    mediaItem.Add(new(MediaItemsViewsVM.RebuildThumbnailsCommand));
    mediaItem.Add(new(MediaItemVM.RenameCommand) { InputGestureText = "F2"});
    mediaItem.Add(new(CoreVM.ResizeImagesCommand));
    mediaItem.Add(new(CoreVM.ReloadMetadataCommand));
    mediaItem.Add(new(CoreVM.RotateMediaItemsCommand) { InputGestureText = "Ctrl+R"});
    mediaItem.Add(new(CoreVM.SaveImageMetadataToFilesCommand));
    mediaItem.Add(new(MediaItemsViewsVM.ViewModifiedCommand));

    var segments = new MenuItem(Res.IconSegment, "Segments");
    segments.Add(new(Features.Segment.SegmentVM.DeleteSelectedCommand));
    segments.Add(new(CoreVM.ExportSegmentsCommand));
    segments.Add(new(CoreVM.OpenSegmentsViewsCommand));

    _initTabs(coreVM.MainWindow.TreeViewCategories, _leftTabs);
    _initTabs(coreVM.MainTabs, _middleTabs);
    _initTabs(coreVM.ToolsTabs, _rightTabs);

    RootHolder.Add(geoLocation);
    RootHolder.Add(mediaItem);
    RootHolder.Add(segments);
    RootHolder.Add(_leftTabs);
    RootHolder.Add(_middleTabs);
    RootHolder.Add(_rightTabs);
    RootHolder.Add(new MenuItem(CoreVM.SaveDbCommand));
    RootHolder.Add(new MenuItem(CoreVM.OpenSettingsCommand));
    RootHolder.Add(new MenuItem(WhatIsNewVM.OpenCommand));
    RootHolder.Add(new MenuItem(CoreVM.OpenAboutCommand));
  }

  private void _initTabs(TabControl tabControl, MenuItem root) {
    foreach (var tab in tabControl.Tabs)
      _addTab(tabControl, tab, root);

    tabControl.Tabs.CollectionChanged += (_, e) => _onTabsChanged(tabControl, root, e);
  }

  private void _addTab(TabControl tabControl, IListItem tab, MenuItem root) {
    tab.PropertyChanged += (_, e) => {
      if (!e.Is(nameof(IListItem.Name))) return;
      root.ReplaceWithData(tab, _createTabMenuItem(tabControl, tab));
    };

    root.Add(_createTabMenuItem(tabControl, tab));
  }

  private MenuItem _createTabMenuItem(TabControl tabControl, IListItem tab) =>
    new(new RelayCommand<IListItem>(
      x => tabControl.Activate(x!.Data!),
      x => x is { Data : { } },
      tab.Icon,
      tab.Name), tab) { Data = tab };

  private void _onTabsChanged(TabControl tabControl, MenuItem root, NotifyCollectionChangedEventArgs e) {
    switch (e.Action) {
      case NotifyCollectionChangedAction.Add:
        if (e.NewItems == null) return;
        foreach (var tab in e.NewItems)
          _addTab(tabControl, (IListItem)tab, root);
        break;
      case NotifyCollectionChangedAction.Remove:
        if (e.OldItems == null) return;
        foreach (var tab in e.OldItems)
          root.RemoveWithData(tab);
        break;
      default:
        root.Items.Clear();
        foreach (var tab in tabControl.Tabs)
          _addTab(tabControl, tab, root);
        break;
    }
  }
}