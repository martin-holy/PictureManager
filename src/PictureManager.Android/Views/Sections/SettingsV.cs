using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Binding;
using MH.UI.Android.Controls;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.UI.Controls;
using MH.Utils.BaseClasses;
using MH.Utils.Disposables;
using PictureManager.Common.Features.Common;
using PictureManager.Common.Features.MediaItem;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Android.Views.Sections;

public sealed class SettingsV : ScrollView {
  private readonly BindingScope _bindings = new();

  public SettingsV(Context context, AllSettings allSettings) : base(context) {
    var container = new LinearLayout(context) { Orientation = Orientation.Vertical };

    foreach (var group in allSettings.Groups)
      container.AddView(_createGroup(context, group, _bindings), LPU.LinearMatchWrap());

    AddView(container);
  }

  private static LinearLayout _createGroup(Context context, ListItem group, BindingScope bindings) {
    var container = new LinearLayout(context) { Orientation = Orientation.Vertical };
    var header = new IconTextView(context, group.Icon, group.Name) {
      Background = BackgroundFactory.Dark()
    };
    container.AddView(header, LPU.Linear(LPU.Match, DimensU.MenuItemHeight).WithMargin(DimensU.Spacing));

    switch (group.Data) {
      case Settings settings: _createSettings(context, container, settings, bindings); break;
      case CommonSettings common: _createCommonSettings(context, container, common, bindings); break;
      case GeoNameSettings geoName: _createGeoNameSettings(context, container, geoName, bindings); break;
      case MediaItemSettings mi: _createMediaItemSettings(context, container, mi, bindings); break;
      case SegmentSettings segment: _createSegmentSettings(context, container, segment, bindings); break;
      case MediaViewerSettings mediaViewer: _createMediaViewerSettings(context, container, mediaViewer, bindings); break;
    }

    return container;
  }

  private static void _createSettings(Context context, LinearLayout container, Settings settings, BindingScope bindings) {
    var index = container.ChildCount - 1;
    var header = container.GetChildAt(index);
    var saveBtn = new IconButton(context).WithClickCommand(settings.SaveCommand, bindings);
    var frame = new FrameLayout(context);

    container.RemoveViewAt(index);
    frame.AddView(header, LPU.Frame(LPU.Match, DimensU.MenuItemHeight).WithMargin(DimensU.Spacing));
    frame.AddView(saveBtn, LPU.Frame(LPU.Wrap, LPU.Wrap, GravityFlags.Right).WithMargin(DimensU.Spacing));
    container.AddView(frame, LPU.LinearMatchWrap());
  }

  private static void _createCommonSettings(Context context, LinearLayout container, CommonSettings settings, BindingScope bindings) {
    _addViews(container, [
      new TextView(context).BindText(settings, nameof(CommonSettings.JpegQuality), x => x.JpegQuality, x => $"Jpeg quality: {x}", bindings),
      new Slider(context, 80, 95, 1.0).BindProgress(settings, nameof(CommonSettings.JpegQuality), x => x.JpegQuality, (s, v) => s.JpegQuality = v, bindings)
    ]);
  }

  private static void _createGeoNameSettings(Context context, LinearLayout container, GeoNameSettings settings, BindingScope bindings) {
    _addViews(container, [
      new TextView(context) { Text = "Load from web:" },
      new CheckBox(context).BindChecked(settings, nameof(GeoNameSettings.LoadFromWeb), x => x.LoadFromWeb, (s, v) => s.LoadFromWeb = v, bindings),

      new TextView(context) { Text = "User name:" },
      new EditText(context).BindText(settings, nameof(GeoNameSettings.UserName), x => x.UserName, (s, v) => s.UserName = v, bindings)
    ]);
  }

  private static void _createMediaItemSettings(Context context, LinearLayout container, MediaItemSettings settings, BindingScope bindings) {
    var sortFields = MediaItemCollectionView.SortFields.Select(x => new KeyValuePair<string, string>(x.Name, x.Name)).ToArray();
    
    _addViews(container, [
      new TextView(context).BindText(settings, nameof(MediaItemSettings.MediaItemThumbScale), x => x.MediaItemThumbScale, x => $"Media item thumbnail scale: {x:G2}", bindings),
      new Slider(context, 0.2, 2, 0.1).BindProgress(settings, nameof(MediaItemSettings.MediaItemThumbScale), x => x.MediaItemThumbScale, (s, v) => s.MediaItemThumbScale = v, bindings),

      new TextView(context) { Text = "Scroll exactly to MediaItem in thumbnails:" },
      new CheckBox(context).BindChecked(settings, nameof(MediaItemSettings.ScrollExactlyToMediaItem), x => x.ScrollExactlyToMediaItem, (s, v) => s.ScrollExactlyToMediaItem = v, bindings),

      new TextView(context) { Text = "Sort field:" },
      new Spinner(context).BindSelected(settings, nameof(MediaItemSettings.SortField), x => x.SortField, (s, v) => s.SortField = v, sortFields, bindings),

      new TextView(context) { Text = "Sort order:" },
      new Spinner(context).BindSelected(settings, nameof(MediaItemSettings.SortOrder), x => x.SortOrder, (s, v) => s.SortOrder = v, CollectionView.SortOrderTextMap, bindings)
    ]);
  }

  private static void _createSegmentSettings(Context context, LinearLayout container, SegmentSettings settings, BindingScope bindings) {
    _addViews(container, [
      new TextView(context).BindText(settings, nameof(SegmentSettings.GroupSize), x => x.GroupSize, x => $"Group size: {x}", bindings),
      new Slider(context, 100, 1000, 50).BindProgress(settings, nameof(SegmentSettings.GroupSize), x => x.GroupSize, (s, v) => s.GroupSize = v, bindings)
    ]);
  }

  private static void _createMediaViewerSettings(Context context, LinearLayout container, MediaViewerSettings settings, BindingScope bindings) {
    _addViews(container, [
      new TextView(context) { Text = "Expand content to fill:" },
      new CheckBox(context).BindChecked(settings, nameof(MediaViewerSettings.ExpandToFill), x => x.ExpandToFill, (s, v) => s.ExpandToFill = v, bindings)
    ]);
  }

  private static void _addViews(LinearLayout layout, View[] views) {
    foreach (View view in views)
      layout.AddView(view, LPU.LinearMatchWrap().WithMargin(DimensU.Spacing));
  }

  protected override void Dispose(bool disposing) {
    if (disposing) _bindings.Dispose();
    base.Dispose(disposing);
  }
}