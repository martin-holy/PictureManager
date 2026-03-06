using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Common.Features.GeoName;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.Segment;
using PictureManager.Common.Features.WhatIsNew;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;

namespace PictureManager.Common.Layout;

public class MainMenuVM : TreeView {
  private readonly MenuItem _leftTabs = new(Res.IconTabLeft, "Left tabs");
  private readonly MenuItem _middleTabs = new(Res.IconTabMiddle, "Middle tabs");
  private readonly MenuItem _rightTabs = new(Res.IconTabRight, "Right tabs");

  public string Icon { get; } = Res.IconThreeBars;
  public Func<IEnumerable<ITreeItem>> BuildMenu => () => RootHolder;

  public void Build(CoreVM coreVM) {
    RootHolder.Clear();

    var geoLocation = new MenuItem(Res.IconLocationCheckin, "GeoLocation", [
      new MenuItem(CoreVM.GetGeoNamesFromWebCommand),
      new MenuItem(GeoNameVM.NewGeoNameFromGpsCommand),
      new MenuItem(CoreVM.ReadGeoLocationFromFilesCommand)]);

    var mediaItem = new MenuItem(Res.IconImageMultiple, "Media Items", [
      new MenuItem(MediaItemsViewsVM.AddViewCommand),
      new MenuItem(MediaItemVM.CommentCommand) { InputGestureText = "Ctrl+K"},
      new MenuItem(CoreVM.CompressImagesCommand),
      new MenuItem(MediaItemsViewsVM.CopyPathsCommand),
      new MenuItem(MediaItemVM.DeleteCommand),
      new MenuItem(CoreVM.ImagesToVideoCommand),
      new MenuItem(MediaItemsViewsVM.RebuildThumbnailsCommand),
      new MenuItem(MediaItemVM.RenameCommand) { InputGestureText = "F2"},
      new MenuItem(CoreVM.ResizeSelectedImagesCommand),
      new MenuItem(CoreVM.ReloadMetadataCommand),
      new MenuItem(CoreVM.RotateMediaItemsCommand) { InputGestureText = "Ctrl+R"},
      new MenuItem(CoreVM.SaveImageMetadataToFilesCommand),
      new MenuItem(MediaItemsViewsVM.ViewModifiedCommand),
      new MenuItem(MediaItemVM.ViewSelectedCommand)]);

    var segments = new MenuItem(Res.IconSegment, "Segments", [
      new MenuItem(Features.Segment.SegmentVM.DeleteSelectedCommand),
      new MenuItem(CoreVM.ExportSegmentsCommand),
      new MenuItem(CoreVM.OpenSegmentsViewsCommand),
      new MenuItem(SegmentVM.SetSelectedAsSamePersonCommand),
      new MenuItem(SegmentVM.SetSelectedAsUnknownCommand),
      new MenuItem(SegmentVM.AddEmptyViewCommand),
      new MenuItem(SegmentsDrawerVM.OpenCommand),
      new MenuItem(SegmentsDrawerVM.AddSelectedCommand),
      new MenuItem(SegmentsDrawerVM.RemoveSelectedCommand)
    ]);

    _initTabs(coreVM.MainWindow.TreeViewCategories, _leftTabs);
    _initTabs(coreVM.MainTabs, _middleTabs);
    _initTabs(coreVM.ToolsTabs, _rightTabs);

    RootHolder.Add(geoLocation);
    RootHolder.Add(mediaItem);
    RootHolder.Add(segments);
    RootHolder.Add(new MenuItemSeparator());
    RootHolder.Add(_leftTabs);
    RootHolder.Add(_middleTabs);
    RootHolder.Add(_rightTabs);
    RootHolder.Add(new MenuItemSeparator());
    RootHolder.Add(new MenuItem(CoreVM.SaveDbCommand));
    RootHolder.Add(new MenuItem(CoreVM.OpenSettingsCommand));
    RootHolder.Add(new MenuItem(WhatIsNewVM.OpenCommand));
    RootHolder.Add(new MenuItem(CoreVM.OpenAboutCommand));
  }

  public void HideMenuItems(ICommand[] commands) {
    foreach (var menuItem in RootHolder.Flatten().OfType<MenuItem>())
      if (commands.Contains(menuItem.Command))
        menuItem.IsHidden = true;
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