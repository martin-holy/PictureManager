using Android.Content;
using Android.Views;
using MH.UI.Android.Dialogs;
using MH.UI.Controls;
using MH.UI.Dialogs;
using MH.Utils.Disposables;
using PictureManager.Android.Views.Dialogs;
using PictureManager.Common.Features.Common;
using PictureManager.Common.Features.GeoLocation;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.MediaItem.Image;
using PictureManager.Common.Features.Person;

namespace PictureManager.Android.Utils;

public static class DialogFactory {
  public static View? GetDialog(Context context, Dialog dataContext, BindingScope bindings) {
    return dataContext switch {
      AboutDialog => new AboutDialogV(context, (AboutDialog)dataContext, bindings),
      CompressImagesDialog => new CompressImagesDialogV(context, (CompressImagesDialog)dataContext, bindings),
      ComputeImageHashesDialog => new ProgressDialogV(context, (IProgressDialog)dataContext, bindings),
      FileOperationCollisionDialog => new FileOperationCollisionDialogV(context, (FileOperationCollisionDialog)dataContext, bindings),
      FileOperationDialog => new FileOperationDialogV(context, (FileOperationDialog)dataContext, bindings),
      GetGeoNamesFromWebDialog => new ProgressDialogV(context, (IProgressDialog)dataContext, bindings),
      ImageResizeDialog => new ImageResizeDialogV(context, (ImageResizeDialog)dataContext, bindings),
      MergePeopleDialog => new MergePeopleDialogV(context, (MergePeopleDialog)dataContext, bindings),
      ReadGeoLocationFromFilesDialog => new ProgressDialogV(context, (IProgressDialog)dataContext, bindings),
      ReloadMetadataDialog => new ProgressDialogV(context, (IProgressDialog)dataContext, bindings),
      RotationDialog => new RotationDialogV(context, (RotationDialog)dataContext, bindings),
      SaveMetadataDialog => new ProgressDialogV(context, (IProgressDialog)dataContext, bindings),
      _ => null
    };
  }
}