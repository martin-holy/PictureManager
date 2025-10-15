using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Common.Features.Common;

namespace PictureManager.Android.Views.Sections;

public sealed class SettingsV : LinearLayout {
  public SettingsV(Context context, AllSettings allSettings) : base(context) {
    Orientation = Orientation.Vertical;

    foreach (var group in allSettings.Groups)
      AddView(_createGroup(context, group), new LayoutParams(LPU.Match, LPU.Wrap));
  }

  private static LinearLayout _createGroup(Context context, ListItem group) {
    var container = new LinearLayout(context) { Orientation = Orientation.Vertical };
    var header = new IconTextView(context).BindIcon(group.Icon).BindText(group.Name);
    header.Background = BackgroundFactory.Dark();
    container.AddView(header, new LayoutParams(LPU.Match, DimensU.MenuItemHeight).WithMargin(DimensU.Spacing));

    switch (group.Data) {
      case Settings settings: _createSettings(context, container, settings); break;
      case CommonSettings common: _createCommonSettings(context, container, common); break;
      case MediaItemSettings mi: _createMediaItemSettings(context, container, mi); break;
      case SegmentSettings segment: _createSegmentSettings(context, container, segment); break;
    }

    return container;
  }

  private static void _createSettings(Context context, LinearLayout container, Settings settings) {
    var index = container.ChildCount - 1;
    var header = container.GetChildAt(index);
    var saveBtn = new IconButton(context).WithCommand(settings.SaveCommand);
    var frame = new FrameLayout(context);

    container.RemoveViewAt(index);
    frame.AddView(header, new FrameLayout.LayoutParams(LPU.Match, DimensU.MenuItemHeight).WithMargin(DimensU.Spacing));
    frame.AddView(saveBtn, new FrameLayout.LayoutParams(LPU.Wrap, LPU.Wrap, GravityFlags.Right).WithMargin(DimensU.Spacing));
    container.AddView(frame, new LayoutParams(LPU.Match, LPU.Wrap));
  }

  private static void _createCommonSettings(Context context, LinearLayout container, CommonSettings settings) {
    _addView(container, new TextView(context).BindText(settings, x => x.JpegQuality, x => $"Jpeg quality: {x}"));
    _addView(container, new Slider(context, 80, 95, 1.0).BindProgress(settings, x => x.JpegQuality));
  }

  private static void _createMediaItemSettings(Context context, LinearLayout container, MediaItemSettings settings) {
    _addView(container, new TextView(context).BindText(settings, x => x.MediaItemThumbScale, x => $"Media item thumbnail scale: {x:G2}"));
    _addView(container, new Slider(context, 0.2, 2, 0.1).BindProgress(settings, x => x.MediaItemThumbScale));
  }

  private static void _createSegmentSettings(Context context, LinearLayout container, SegmentSettings settings) {
    _addView(container, new TextView(context).BindText(settings, x => x.GroupSize, x => $"Group size: {x}"));
    _addView(container, new Slider(context, 100, 1000, 50).BindProgress(settings, x => x.GroupSize));
  }

  private static void _addView(LinearLayout container, View view) =>
    container.AddView(view, new LayoutParams(LPU.Match, LPU.Wrap).WithMargin(DimensU.Spacing));
}