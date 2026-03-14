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

    _addViews(this, [fromText, fromPath, toText, toPath, fileText, fileName, progress]);
  }

  private static void _addViews(LinearLayout layout, View[] views) {
    foreach (View view in views)
      layout.AddView(view, LPU.LinearWrap().WithMargin(DimensU.Spacing));
  }
}