using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.Utils;
using PictureManager.Common.Features.MediaItem.Image;

namespace PictureManager.Android.Views.Dialogs;

public sealed class CompressImagesDialogV : LinearLayout {
  private readonly CompressImagesDialog _dataContext;

  public CompressImagesDialogV(Context context, CompressImagesDialog dataContext) : base(context) {
    _dataContext = dataContext;
    Orientation = Orientation.Vertical;

    AddView(_createQualityLayout(), new LayoutParams(LPU.Match, LPU.Wrap));
    AddView(_createProgressSizesLayout(), new LayoutParams(LPU.Match, DisplayU.DpToPx(64)));
    AddView(_createProgressTextLayout(), new LayoutParams(LPU.Match, LPU.Wrap).WithMargin(DimensU.Spacing));
    AddView(_createProgressBarLayout(), new LayoutParams(DisplayU.DpToPx(256), LPU.Wrap).WithMargin(DimensU.Spacing));
  }

  private LinearLayout _createQualityLayout() {
    var layout = new LinearLayout(Context) { Orientation = Orientation.Horizontal };

    layout.AddView(new TextView(Context)
      .BindText(_dataContext, nameof(CompressImagesDialog.JpegQualityLevel),
        x => x.JpegQualityLevel, q => $"JPG quality level: {q}", out var _),
        new LinearLayout.LayoutParams(LPU.Wrap, LPU.Wrap));

    layout.AddView(new Slider(Context, 70, 95, 1)
      .BindProgress(_dataContext, nameof(CompressImagesDialog.JpegQualityLevel),
      x => x.JpegQualityLevel, (s, p) => s.JpegQualityLevel = p, out var _),
      new LinearLayout.LayoutParams(0, LPU.Wrap, 1f));

    return layout;
  }

  private LinearLayout _createProgressSizesLayout() {
    var originalLayout = new LinearLayout(Context) { Orientation = Orientation.Vertical };
    originalLayout.AddView(new TextView(Context) { Text = "Original" },
      new LinearLayout.LayoutParams(LPU.Wrap, LPU.Wrap) { Gravity = GravityFlags.CenterHorizontal });
    originalLayout.AddView(new TextView(Context) { TextSize = 20 }
      .BindText(_dataContext, nameof(CompressImagesDialog.TotalSourceSize), x => x.TotalSourceSize, x => x, out var _),
      new LinearLayout.LayoutParams(LPU.Wrap, LPU.Wrap) { Gravity = GravityFlags.CenterHorizontal });

    var compressedLayout = new LinearLayout(Context) { Orientation = Orientation.Vertical };
    compressedLayout.AddView(new TextView(Context) { Text = "Compressed" },
      new LinearLayout.LayoutParams(LPU.Wrap, LPU.Wrap) { Gravity = GravityFlags.CenterHorizontal });
    compressedLayout.AddView(new TextView(Context) { TextSize = 20 }
      .BindText(_dataContext, nameof(CompressImagesDialog.TotalCompressedSize), x => x.TotalCompressedSize, x => x, out var _),
      new LinearLayout.LayoutParams(LPU.Wrap, LPU.Wrap) { Gravity = GravityFlags.CenterHorizontal });

    var layout = new LinearLayout(Context) { Orientation = Orientation.Horizontal };
    layout.AddView(originalLayout, new LinearLayout.LayoutParams(0, LPU.Match, 1f));
    layout.AddView(compressedLayout, new LinearLayout.LayoutParams(0, LPU.Match, 1f));

    return layout;
  }

  private TextView? _createProgressTextLayout() =>
    new TextView(Context)
      .BindText(_dataContext, nameof(CompressImagesDialog.ProgressText), x => x.ProgressText, x => x ?? string.Empty, out var _);

  private ProgressBar? _createProgressBarLayout() {
    var progress = new ProgressBar(Context) { Max = _dataContext.ProgressMax };
    progress.Bind(_dataContext, nameof(CompressImagesDialog.ProgressValue), x => x.ProgressValue, (s, v) => s.Progress = v);
    return progress;
  }
}