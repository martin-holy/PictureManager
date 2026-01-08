using Android.Content;
using Android.Widget;
using MH.UI.Android.Utils;
using PictureManager.Common.Features.Common;

namespace PictureManager.Android.Views.Dialogs;

public sealed class FileOperationCollisionDialogV : LinearLayout {
  // TODO
  public FileOperationCollisionDialogV(Context context, FileOperationCollisionDialog dataContext) : base(context) {
    AddView(new EditText(context)
      .BindText(dataContext, nameof(FileOperationCollisionDialog.FileName), x => x.FileName, (s, p) => s.FileName = p, out var _));
  }
}
