using MH.UI.AvaloniaUI.Controls;
using MH.UI.Dialogs;
using PictureManager.Common.Features.GeoLocation;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.MediaItem.Image;

namespace PictureManager.AvaloniaUI.Controls;

public class DialogHostContentTemplateSelector() : TypeDataTemplateSelector(_mappings) {
  private static readonly TypeTemplateMapping[] _mappings = [
    new(typeof(GetGeoNamesFromWebDialog), "MH.UI.Dialogs.ProgressDialog`1"),
    new(typeof(ReadGeoLocationFromFilesDialog), "MH.UI.Dialogs.ProgressDialog`1"),
    new(typeof(ComputeImageHashesDialog), "MH.UI.Dialogs.ProgressDialog`1"),
    new(typeof(ReloadMetadataDialog), "MH.UI.Dialogs.ProgressDialog`1"),
    new(typeof(SaveMetadataDialog), "MH.UI.Dialogs.ProgressDialog`1"),
    new(typeof(GroupByDialog<>), "MH.UI.Dialogs.GroupByDialog`1"),
    new(typeof(ProgressDialog<>), "MH.UI.Dialogs.ProgressDialog`1")
  ];
}