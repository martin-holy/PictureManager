using Android.Content;
using Android.Widget;
using MH.UI.Android.Binding;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.Utils;
using MH.Utils.Disposables;
using PictureManager.Common.Features.Common;

namespace PictureManager.Android.Views.Dialogs;

public sealed class FileOperationDialogV : LinearLayout {
  public FileOperationDialogV(Context context, FileOperationDialog dataContext, BindingScope bindings) : base(context) {
    Orientation = Orientation.Vertical;
    LayoutParameters = new LinearLayout.LayoutParams(LPU.Match, LPU.Wrap);

    AddView(new TextView(context) { Text = "From:" }, new LayoutParams(LPU.Wrap, LPU.Wrap).WithMargin(DimensU.Spacing));
    AddView(new TextView(context)
      .BindText(dataContext, nameof(FileOperationDialog.DirFrom), x => x.DirFrom, x => x, bindings),
      new LayoutParams(LPU.Wrap, LPU.Wrap).WithMargin(DimensU.Spacing));
    
    AddView(new TextView(context) { Text = "To:" }, new LayoutParams(LPU.Wrap, LPU.Wrap).WithMargin(DimensU.Spacing));
    AddView(new TextView(context)
      .BindText(dataContext, nameof(FileOperationDialog.DirTo), x => x.DirTo, x => x, bindings),
      new LayoutParams(LPU.Wrap, LPU.Wrap).WithMargin(DimensU.Spacing));
    
    AddView(new TextView(context) { Text = "File:" }, new LayoutParams(LPU.Wrap, LPU.Wrap).WithMargin(DimensU.Spacing));
    AddView(new TextView(context)
      .BindText(dataContext, nameof(FileOperationDialog.FileName), x => x.FileName, x => x, bindings),
      new LayoutParams(LPU.Wrap, LPU.Wrap).WithMargin(DimensU.Spacing));

    var progress = new ProgressBar(Context) { Max = 100 };
    dataContext.Bind(nameof(FileOperationDialog.ProgressValue), x => x.ProgressValue, v => progress.Progress = v).DisposeWith(bindings);
    dataContext.Bind(nameof(FileOperationDialog.IsIndeterminate), x => x.IsIndeterminate, v => progress.Indeterminate = v).DisposeWith(bindings);
    AddView(progress, new LayoutParams(LPU.Wrap, LPU.Wrap).WithMargin(DimensU.Spacing));
  }
}