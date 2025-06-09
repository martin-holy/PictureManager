using MH.Utils.BaseClasses;
using PictureManager.Common.Features.GeoName;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.WhatIsNew;

namespace PictureManager.Common.Layout;

public class MainMenuVM {
  public MenuItem Root { get; } = new MenuItem(Res.IconThreeBars, string.Empty);

  public void Build() {
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

    Root.Add(geoLocation);
    Root.Add(mediaItem);
    Root.Add(segments);
    Root.Add(new(CoreVM.OpenSettingsCommand));
    Root.Add(new(WhatIsNewVM.OpenCommand));
    Root.Add(new(CoreVM.OpenAboutCommand));
  }
}