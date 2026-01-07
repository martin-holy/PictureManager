using Android.Content;
using Android.Widget;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.Utils;
using PictureManager.Common.Features.Common;

namespace PictureManager.Android.Views.Dialogs;

public sealed class FileOperationDialogV : LinearLayout {
  public FileOperationDialogV(Context context, FileOperationDialog dataContext) : base(context) {
    Orientation = Orientation.Vertical;
    LayoutParameters = new LinearLayout.LayoutParams(LPU.Match, LPU.Wrap);

    AddView(new TextView(context) { Text = "From:" }, new LayoutParams(LPU.Wrap, LPU.Wrap).WithMargin(DimensU.Spacing));
    AddView(new TextView(context)
      .BindText(dataContext, nameof(FileOperationDialog.DirFrom), x => x.DirFrom, x => x, out var _),
      new LayoutParams(LPU.Wrap, LPU.Wrap).WithMargin(DimensU.Spacing));
    
    AddView(new TextView(context) { Text = "To:" }, new LayoutParams(LPU.Wrap, LPU.Wrap).WithMargin(DimensU.Spacing));
    AddView(new TextView(context)
      .BindText(dataContext, nameof(FileOperationDialog.DirTo), x => x.DirTo, x => x, out var _),
      new LayoutParams(LPU.Wrap, LPU.Wrap).WithMargin(DimensU.Spacing));
    
    AddView(new TextView(context) { Text = "File:" }, new LayoutParams(LPU.Wrap, LPU.Wrap).WithMargin(DimensU.Spacing));
    AddView(new TextView(context)
      .BindText(dataContext, nameof(FileOperationDialog.FileName), x => x.FileName, x => x, out var _),
      new LayoutParams(LPU.Wrap, LPU.Wrap).WithMargin(DimensU.Spacing));

    var progress = new ProgressBar(Context) { Max = 100 };
    progress.Bind(dataContext, nameof(FileOperationDialog.ProgressValue), x => x.ProgressValue, (s, v) => s.Progress = v);
    progress.Bind(dataContext, nameof(FileOperationDialog.IsIndeterminate), x => x.IsIndeterminate, (s, v) => s.Indeterminate = v);
    AddView(progress, new LayoutParams(LPU.Wrap, LPU.Wrap).WithMargin(DimensU.Spacing));
  }
}