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
    const int jpgQmin = 80, jpgQmax = 95;

    var jpgQText = new TextView(context)
      .WithBind(settings, x => x.JpegQuality, (v, p) => v.Text = $"Jpeg quality: {p}");

    var jpgQValue = new SeekBar(context) { Max = jpgQmax - jpgQmin, Progress = settings.JpegQuality - jpgQmin };
    jpgQValue.ProgressChanged += (s, e) => settings.JpegQuality = e.Progress + jpgQmin;

    container.AddView(jpgQText, new LayoutParams(LPU.Match, LPU.Wrap).WithMargin(DimensU.Spacing));
    container.AddView(jpgQValue, new LayoutParams(LPU.Match, LPU.Wrap).WithMargin(DimensU.Spacing));
  }

  private static void _createMediaItemSettings(Context context, LinearLayout container, MediaItemSettings settings) {
    const double miThumbScaleMin = 0.2, miThumbScaleMax = 2;

    var miThumbScaleText = new TextView(context)
      .WithBind(settings, x => x.MediaItemThumbScale, (v, p) => v.Text = $"Media item thumbnail scale: {p:G2}");

    var miThumbScaleValue = new SeekBar(context) {
      Max = (int)((miThumbScaleMax - miThumbScaleMin) * 10),
      Progress = (int)((settings.MediaItemThumbScale - miThumbScaleMin) * 10) };
    miThumbScaleValue.ProgressChanged += (s, e) => settings.MediaItemThumbScale = e.Progress / 10.0 + miThumbScaleMin;

    container.AddView(miThumbScaleText, new LayoutParams(LPU.Match, LPU.Wrap).WithMargin(DimensU.Spacing));
    container.AddView(miThumbScaleValue, new LayoutParams(LPU.Match, LPU.Wrap).WithMargin(DimensU.Spacing));
  }
}
