using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Utils;
using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Common.Features.Common;
using MH.UI.Android.Extensions;

namespace PictureManager.Android.Views.Sections;

public sealed class SettingsV : LinearLayout {
  public SettingsV(Context context, AllSettings allSettings) : base(context) {
    Orientation = Orientation.Vertical;

    foreach (var group in allSettings.Groups)
      AddView(_createGroup(context, group), new LayoutParams(LPU.Match, LPU.Wrap));
  }

  private View _createGroup(Context context, ListItem group) {
    var container = new LinearLayout(context) { Orientation = Orientation.Vertical };
    var header = new IconTextView(context).BindIcon(group.Icon).BindText(group.Name);
    container.AddView(header, new LayoutParams(LPU.Match, LPU.Wrap));

    switch (group.Data) {
      case CommonSettings common: _createCommonSettings(context, container, common); break;
    }

    return container;
  }

  private void _createCommonSettings(Context context, LinearLayout container, CommonSettings settings) {
    const int jpgQmin = 80, jpgQmax = 95;

    var jpgQText = new TextView(context)
      .WithBind(settings, x => x.JpegQuality, (v, p) => v.Text = $"Jpeg quality: {p}");

    var jpgQValue = new SeekBar(context) { Max = jpgQmax - jpgQmin, Progress = settings.JpegQuality - jpgQmin };
    jpgQValue.ProgressChanged += (s, e) => settings.JpegQuality = e.Progress + jpgQmin;

    container.AddView(jpgQText);
    container.AddView(jpgQValue, new LayoutParams(LPU.Match, LPU.Wrap).WithMargin(DimensU.Spacing));
  }
}
