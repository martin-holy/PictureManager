using Android.Content;
using Android.Views;
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
    LayoutParameters = LPU.LinearMatchWrap();

    var fromText = new TextView(context) { Text = "From:" };
    var fromPath = new TextView(context)
      .BindText(dataContext, nameof(FileOperationDialog.DirFrom), x => x.DirFrom, x => x, bindings);

    var toText = new TextView(context) { Text = "To:" };
    var toPath = new TextView(context)
      .BindText(dataContext, nameof(FileOperationDialog.DirTo), x => x.DirTo, x => x, bindings);

    var fileText = new TextView(context) { Text = "File:" };
    var fileName = new TextView(context)
      .BindText(dataContext, nameof(FileOperationDialog.FileName), x => x.FileName, x => x, bindings);

    var progress = new ProgressBar(Context) { Max = 100 };
    dataContext.Bind(nameof(FileOperationDialog.ProgressValue), x => x.ProgressValue, v => progress.Progress = v).DisposeWith(bindings);
    dataContext.Bind(nameof(FileOperationDialog.IsIndeterminate), x => x.IsIndeterminate, v => progress.Indeterminate = v).DisposeWith(bindings);

    var labels = LayoutU.Vertical(context)
      .Add(fromText, LPU.LinearWrap().WithMargin(DimensU.Spacing))
      .Add(toText, LPU.LinearWrap().WithMargin(DimensU.Spacing))
      .Add(fileText, LPU.LinearWrap().WithMargin(DimensU.Spacing));

    var data = LayoutU.Vertical(context)
      .Add(fromPath, LPU.LinearWrap().WithMargin(DimensU.Spacing))
      .Add(toPath, LPU.LinearWrap().WithMargin(DimensU.Spacing))
      .Add(fileName, LPU.LinearWrap().WithMargin(DimensU.Spacing));

    var labelsAndData = LayoutU.Horizontal(context)
      .Add(labels, LPU.LinearWrap())
      .Add(data, LPU.LinearWrap());

    AddView(labelsAndData, LPU.LinearWrap());
    AddView(progress, LPU.Linear(LPU.Match, LPU.Wrap).WithMargin(DimensU.Spacing));
  }
}