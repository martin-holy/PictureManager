using Android.Content;
using Android.Widget;
using MH.UI.Android.Binding;
using MH.UI.Android.Controls;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.Utils.Disposables;
using PictureManager.Common.Features.MediaItem.Image;

namespace PictureManager.Android.Views.Dialogs;

public sealed class ImageResizeDialogV : LinearLayout {
  private readonly ImageResizeDialog _dataContext;
  private readonly BindingScope _bindings;

  public ImageResizeDialogV(Context context, ImageResizeDialog dataContext, BindingScope bindings) : base(context) {
    _dataContext = dataContext;
    _bindings = bindings;
    Orientation = Orientation.Vertical;
    LayoutParameters = LPU.Linear(LPU.Match, 0, 1f);

    var skipIf = new CheckBox(Context) { Text = "Skip if file exists" }
      .BindChecked(_dataContext, nameof(ImageResizeDialog.SkipIfExists),
        x => x.SkipIfExists, (s, v) => s.SkipIfExists = v, bindings);

    var progressText = new TextView(context).BindProgressText(dataContext, bindings);
    var progressBar = new ProgressBar(context).BindProgressBar(dataContext, bindings);

    AddView(_optionsLayout(), LPU.LinearWrap().WithMargin(DimensU.Spacing));
    AddView(_sizeLayout(), LPU.LinearMatchWrap().WithMargin(DimensU.Spacing));
    AddView(skipIf, LPU.LinearWrap().WithMargin(DimensU.Spacing));
    AddView(progressText, LPU.LinearMatchWrap().WithMargin(DimensU.Spacing));
    AddView(progressBar, LPU.LinearMatchWrap().WithMargin(DimensU.Spacing));
  }

  private LinearLayout _optionsLayout() {
    var preserve = new TextView(Context) { Text = "Preserve:" };
    
    var metadata = new CheckBox(Context) { Text = "Metadata" }
      .BindChecked(_dataContext, nameof(ImageResizeDialog.PreserveMetadata),
      x => x.PreserveMetadata, (s, v) => s.PreserveMetadata = v, _bindings);
    
    var folders = new CheckBox(Context) { Text = "Folders" }
      .BindChecked(_dataContext, nameof(ImageResizeDialog.PreserveFolders),
      x => x.PreserveFolders, (s, v) => s.PreserveFolders = v, _bindings);

    return LayoutU.Horizontal(Context)
      .Add(preserve, LPU.LinearWrap())
      .Add(metadata, LPU.LinearWrap())
      .Add(folders, LPU.LinearWrap());
  }

  private LinearLayout _sizeLayout() {
    var text = new TextView(Context)
      .BindText(_dataContext, nameof(ImageResizeDialog.Mpx), x => x.Mpx, mpx => $"{mpx:F1} MPx", _bindings);
    
    var slider = new Slider(Context, 0.1, _dataContext.MaxMpx, 0.1)
      .BindProgress(_dataContext, nameof(ImageResizeDialog.Mpx), x => x.Mpx, (s, p) => s.Mpx = p, _bindings);

    return LayoutU.Horizontal(Context)
      .Add(text, LPU.LinearWrap())
      .Add(slider, LPU.Linear(0, LPU.Wrap, 1f));
  }
}