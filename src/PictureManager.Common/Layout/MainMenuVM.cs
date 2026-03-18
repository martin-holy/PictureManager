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
using System.Linq;
using System.Windows.Input;

namespace PictureManager.Common.Layout;

public class MainMenuVM : TreeView {
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

    RootHolder.Add(geoLocation);
    RootHolder.Add(mediaItem);
    RootHolder.Add(segments);
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
}