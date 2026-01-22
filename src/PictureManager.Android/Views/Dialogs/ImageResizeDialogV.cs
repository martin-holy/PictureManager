using Android.Content;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using PictureManager.Common.Features.MediaItem.Image;

namespace PictureManager.Android.Views.Dialogs;

public sealed class ImageResizeDialogV : LinearLayout {
  private readonly ImageResizeDialog _dataContext;

  public ImageResizeDialogV(Context context, ImageResizeDialog dataContext) : base(context) {
    _dataContext = dataContext;
    Orientation = Orientation.Vertical;
    LayoutParameters = new LayoutParams(LPU.Match, 0, 1f);

    AddView(_createOptionsLayout(), new LayoutParams(LPU.Wrap, LPU.Wrap).WithMargin(DimensU.Spacing));
    AddView(_createSizeLayout(), new LayoutParams(LPU.Match, LPU.Wrap).WithMargin(DimensU.Spacing));
    
    AddView(new CheckBox(Context) { Text = "Skip if file exists" }
      .BindChecked(_dataContext, nameof(ImageResizeDialog.SkipIfExists),
        x => x.SkipIfExists, (s, v) => s.SkipIfExists = v, out var _),
      new LayoutParams(LPU.Wrap, LPU.Wrap).WithMargin(DimensU.Spacing));
    
    AddView(new TextView(context).BindProgressText(dataContext, out var _),
      new LayoutParams(LPU.Match, LPU.Wrap).WithMargin(DimensU.Spacing));
    
    AddView(new ProgressBar(context).BindProgressBar(dataContext, out var _),
      new LayoutParams(LPU.Match, LPU.Wrap).WithMargin(DimensU.Spacing));
  }

  private LinearLayout _createOptionsLayout() {
    var container = new LinearLayout(Context) { Orientation = Orientation.Horizontal };

    container.AddView(new TextView(Context) { Text = "Preserve:" });
    
    container.AddView(new CheckBox(Context) { Text = "Metadata" }
      .BindChecked(_dataContext, nameof(ImageResizeDialog.PreserveMetadata),
      x => x.PreserveMetadata, (s, v) => s.PreserveMetadata = v, out var _));
    
    container.AddView(new CheckBox(Context) { Text = "Folders" }
      .BindChecked(_dataContext, nameof(ImageResizeDialog.PreserveFolders),
      x => x.PreserveFolders, (s, v) => s.PreserveFolders = v, out var _));

    return container;
  }

  private LinearLayout _createSizeLayout() {
    var layout = new LinearLayout(Context) { Orientation = Orientation.Horizontal };

    layout.AddView(new TextView(Context)
      .BindText(_dataContext, nameof(ImageResizeDialog.Mpx),
        x => x.Mpx, mpx => $"{mpx:F1} MPx", out var _),
        new LinearLayout.LayoutParams(LPU.Wrap, LPU.Wrap));

    layout.AddView(new Slider(Context, 0.1, _dataContext.MaxMpx, 0.1)
      .BindProgress(_dataContext, nameof(ImageResizeDialog.Mpx),
        x => x.Mpx, (s, p) => s.Mpx = p, out var _),
        new LinearLayout.LayoutParams(0, LPU.Wrap, 1f));

    return layout;
  }
}
