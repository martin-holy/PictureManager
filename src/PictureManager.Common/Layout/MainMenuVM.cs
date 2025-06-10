using MH.UI.Controls;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Common.Features.GeoName;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.WhatIsNew;
using System.Collections.Specialized;

namespace PictureManager.Common.Layout;

public class MainMenuVM {
  private readonly MenuItem _leftTabs = new(Res.IconTabLeft, "Left tabs");
  private readonly MenuItem _middleTabs = new(Res.IconTabMiddle, "Middle tabs");
  private readonly MenuItem _rightTabs = new(Res.IconTabRight, "Right tabs");

  public MenuItem Root { get; } = new MenuItem(Res.IconThreeBars, string.Empty);

  public void Build(CoreVM coreVM) {
    var geoLocation = new MenuItem(Res.IconLocationCheckin, "GeoLocation");
    geoLocation.Add(new(CoreVM.GetGeoNamesFromWebCommand));
    geoLocation.Add(new(GeoNameVM.NewGeoNameFromGpsCommand));
    geoLocation.Add(new(CoreVM.ReadGeoLocationFromFilesCommand));

    var mediaItem = new MenuItem(Res.IconImageMultiple, "Media Items");
    mediaItem.Add(new(MediaItemVM.CommentCommand) { InputGestureText = "Ctrl+K"});
    mediaItem.Add(new(CoreVM.CompressImagesCommand));
    mediaItem.Add(new(MediaItemsViewsVM.CopyPathsCommand));
    mediaItem.Add(new(CoreVM.ImagesToVideoCommand));
    mediaItem.Add(new(MediaItemsViewsVM.RebuildThumbnailsCommand));
    mediaItem.Add(new(MediaItemVM.RenameCommand) { InputGestureText = "F2"});
    mediaItem.Add(new(CoreVM.ResizeImagesCommand));
    mediaItem.Add(new(CoreVM.ReloadMetadataCommand));
    mediaItem.Add(new(CoreVM.RotateMediaItemsCommand) { InputGestureText = "Ctrl+R"});
    mediaItem.Add(new(CoreVM.SaveImageMetadataToFilesCommand));
    mediaItem.Add(new(MediaItemsViewsVM.ViewModifiedCommand));

    var segments = new MenuItem(Res.IconSegment, "Segments");
    segments.Add(new(CoreVM.ExportSegmentsCommand));
    segments.Add(new(CoreVM.OpenSegmentsViewsCommand));

    _initTabs(coreVM.MainWindow.TreeViewCategories, _leftTabs);
    _initTabs(coreVM.MainTabs, _middleTabs);
    _initTabs(coreVM.ToolsTabs, _rightTabs);

    Root.Add(geoLocation);
    Root.Add(mediaItem);
    Root.Add(segments);
    Root.Add(_leftTabs);
    Root.Add(_middleTabs);
    Root.Add(_rightTabs);
    Root.Add(new(CoreVM.OpenSettingsCommand));
    Root.Add(new(WhatIsNewVM.OpenCommand));
    Root.Add(new(CoreVM.OpenAboutCommand));
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
      x => tabControl.Activate(((IListItem)x!.Data!).Data!),
      x => x is { Data : IListItem { Data : { } } },
      tab.Icon,
      tab.Name)) { Data = tab };

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