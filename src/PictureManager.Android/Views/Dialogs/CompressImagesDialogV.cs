using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Binding;
using MH.UI.Android.Controls;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.Utils.Disposables;
using PictureManager.Common.Features.MediaItem.Image;
using System;

namespace PictureManager.Android.Views.Dialogs;

public sealed class CompressImagesDialogV : LinearLayout {
  private readonly CompressImagesDialog _dataContext;
  private readonly BindingScope _bindings;

  public CompressImagesDialogV(Context context, CompressImagesDialog dataContext, BindingScope bindings) : base(context) {
    _dataContext = dataContext;
    _bindings = bindings;
    Orientation = Orientation.Vertical;

    var progressText = new TextView(context).BindProgressText(dataContext, bindings);
    var progressBar = new ProgressBar(context).BindProgressBar(dataContext, bindings);

    AddView(_qualityLayout(), LPU.LinearMatchWrap());
    AddView(_comparisonLayout(), LPU.Linear(LPU.Match, DisplayU.DpToPx(64)));
    AddView(progressText, LPU.LinearMatchWrap().WithMargin(DimensU.Spacing));
    AddView(progressBar, LPU.Linear(DisplayU.DpToPx(256), LPU.Wrap).WithMargin(DimensU.Spacing));
  }

  private LinearLayout _qualityLayout() {
    var text = new TextView(Context)
      .BindText(_dataContext, nameof(CompressImagesDialog.JpegQualityLevel),
        x => x.JpegQualityLevel, q => $"JPG quality level: {q}", _bindings);

    var slider = new Slider(Context, 70, 95, 1)
      .BindProgress(_dataContext, nameof(CompressImagesDialog.JpegQualityLevel),
        x => x.JpegQualityLevel, (s, p) => s.JpegQualityLevel = p, _bindings);

    return LayoutU.Horizontal(Context)
      .Add(text, LPU.LinearWrap())
      .Add(slider, LPU.Linear(0, LPU.Wrap, 1f));
  }

  private LinearLayout _comparisonLayout() {
    var original = _sizeLayout("Original", nameof(CompressImagesDialog.TotalSourceSize), x => x.TotalSourceSize);
    var compresssed = _sizeLayout("Compressed", nameof(CompressImagesDialog.TotalCompressedSize), x => x.TotalCompressedSize);

    return LayoutU.Horizontal(Context)
      .Add(original, LPU.Linear(0, LPU.Match, 1f))
      .Add(compresssed, LPU.Linear(0, LPU.Match, 1f));
  }

  private LinearLayout _sizeLayout(string text, string propertyName, Func<CompressImagesDialog, string> getter) {
    var label = new TextView(Context) { Text = text };
    var size = new TextView(Context) { TextSize = 20 }
      .BindText(_dataContext, propertyName, getter, x => x, _bindings);

    return LayoutU.Vertical(Context)
      .Add(label, LPU.LinearWrap(GravityFlags.CenterHorizontal))
      .Add(size, LPU.LinearWrap(GravityFlags.CenterHorizontal));
  }
}