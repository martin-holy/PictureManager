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
      container.AddView(_createGroup(context, group, _bindings), new LinearLayout.LayoutParams(LPU.Match, LPU.Wrap));

    AddView(container);
  }

  private static LinearLayout _createGroup(Context context, ListItem group, BindingScope bindings) {
    var container = new LinearLayout(context) { Orientation = Orientation.Vertical };
    var header = new IconTextView(context, group.Icon, group.Name) {
      Background = BackgroundFactory.Dark()
    };
    container.AddView(header, new LinearLayout.LayoutParams(LPU.Match, DimensU.MenuItemHeight).WithMargin(DimensU.Spacing));

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
    frame.AddView(header, new FrameLayout.LayoutParams(LPU.Match, DimensU.MenuItemHeight).WithMargin(DimensU.Spacing));
    frame.AddView(saveBtn, new FrameLayout.LayoutParams(LPU.Wrap, LPU.Wrap, GravityFlags.Right).WithMargin(DimensU.Spacing));
    container.AddView(frame, new LinearLayout.LayoutParams(LPU.Match, LPU.Wrap));
  }

  private static void _createCommonSettings(Context context, LinearLayout container, CommonSettings settings, BindingScope bindings) {
    _addView(container, new TextView(context).BindText(settings, nameof(CommonSettings.JpegQuality), x => x.JpegQuality, x => $"Jpeg quality: {x}", bindings));
    _addView(container, new Slider(context, 80, 95, 1.0).BindProgress(settings, nameof(CommonSettings.JpegQuality), x => x.JpegQuality, (s, v) => s.JpegQuality = v, bindings));
  }

  private static void _createGeoNameSettings(Context context, LinearLayout container, GeoNameSettings settings, BindingScope bindings) {
    _addView(container, new TextView(context) { Text = "Load from web:" });
    _addView(container, new CheckBox(context).BindChecked(settings, nameof(GeoNameSettings.LoadFromWeb), x => x.LoadFromWeb, (s, v) => s.LoadFromWeb = v, bindings));

    _addView(container, new TextView(context) { Text = "User name:" });
    _addView(container, new EditText(context).BindText(settings, nameof(GeoNameSettings.UserName), x => x.UserName, (s, v) => s.UserName = v, bindings));
  }

  private static void _createMediaItemSettings(Context context, LinearLayout container, MediaItemSettings settings, BindingScope bindings) {
    _addView(container, new TextView(context).BindText(settings, nameof(MediaItemSettings.MediaItemThumbScale), x => x.MediaItemThumbScale, x => $"Media item thumbnail scale: {x:G2}", bindings));
    _addView(container, new Slider(context, 0.2, 2, 0.1).BindProgress(settings, nameof(MediaItemSettings.MediaItemThumbScale), x => x.MediaItemThumbScale, (s, v) => s.MediaItemThumbScale = v, bindings));

    _addView(container, new TextView(context) { Text = "Scroll exactly to MediaItem in thumbnails:" });
    _addView(container, new CheckBox(context).BindChecked(settings, nameof(MediaItemSettings.ScrollExactlyToMediaItem), x => x.ScrollExactlyToMediaItem, (s, v) => s.ScrollExactlyToMediaItem = v, bindings));

    var sortFields = MediaItemCollectionView.SortFields.Select(x => new KeyValuePair<string, string>(x.Name, x.Name)).ToArray();
    _addView(container, new TextView(context) { Text = "Sort field:" });
    _addView(container, new Spinner(context).BindSelected(settings, nameof(MediaItemSettings.SortField), x => x.SortField, (s, v) => s.SortField = v, sortFields, bindings));

    _addView(container, new TextView(context) { Text = "Sort order:" });
    _addView(container, new Spinner(context).BindSelected(settings, nameof(MediaItemSettings.SortOrder), x => x.SortOrder, (s, v) => s.SortOrder = v, CollectionView.SortOrderTextMap, bindings));
  }

  private static void _createSegmentSettings(Context context, LinearLayout container, SegmentSettings settings, BindingScope bindings) {
    _addView(container, new TextView(context).BindText(settings, nameof(SegmentSettings.GroupSize), x => x.GroupSize, x => $"Group size: {x}", bindings));
    _addView(container, new Slider(context, 100, 1000, 50).BindProgress(settings, nameof(SegmentSettings.GroupSize), x => x.GroupSize, (s, v) => s.GroupSize = v, bindings));
  }

  private static void _createMediaViewerSettings(Context context, LinearLayout container, MediaViewerSettings settings, BindingScope bindings) {
    _addView(container, new TextView(context) { Text = "Expand content to fill:" });
    _addView(container, new CheckBox(context).BindChecked(settings, nameof(MediaViewerSettings.ExpandToFill), x => x.ExpandToFill, (s, v) => s.ExpandToFill = v, bindings));

    _addView(container, new TextView(context) { Text = "Shrink content to fill:" });
    _addView(container, new CheckBox(context).BindChecked(settings, nameof(MediaViewerSettings.ShrinkToFill), x => x.ShrinkToFill, (s, p) => s.ShrinkToFill = p, bindings));
  }

  private static void _addView(LinearLayout container, View view) =>
    container.AddView(view, new LinearLayout.LayoutParams(LPU.Match, LPU.Wrap).WithMargin(DimensU.Spacing));

  protected override void Dispose(bool disposing) {
    if (disposing) _bindings.Dispose();
    base.Dispose(disposing);
  }
}