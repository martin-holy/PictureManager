using Android.Content;
using Android.Views;
using MH.UI.Android.Dialogs;
using MH.UI.Controls;
using MH.UI.Dialogs;
using PictureManager.Android.Views.Dialogs;
using PictureManager.Common.Features.Common;
using PictureManager.Common.Features.GeoLocation;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.MediaItem.Image;
using PictureManager.Common.Features.Person;

namespace PictureManager.Android.Utils;

public static class DialogFactory {
  public static View? GetDialog(Context context, Dialog dataContext) {
    return dataContext switch {
      AboutDialog => new AboutDialogV(context, (AboutDialog)dataContext),
      CompressImagesDialog => new CompressImagesDialogV(context, (CompressImagesDialog)dataContext),
      ComputeImageHashesDialog => new ProgressDialogV(context, (IProgressDialog)dataContext),
      FileOperationCollisionDialog => new FileOperationCollisionDialogV(context, (FileOperationCollisionDialog)dataContext),
      FileOperationDialog => new FileOperationDialogV(context, (FileOperationDialog)dataContext),
      GetGeoNamesFromWebDialog => new ProgressDialogV(context, (IProgressDialog)dataContext),
      ImageResizeDialog => new ImageResizeDialogV(context, (ImageResizeDialog)dataContext),
      MergePeopleDialog => new MergePeopleDialogV(context, (MergePeopleDialog)dataContext),
      ReadGeoLocationFromFilesDialog => new ProgressDialogV(context, (IProgressDialog)dataContext),
      ReloadMetadataDialog => new ProgressDialogV(context, (IProgressDialog)dataContext),
      SaveMetadataDialog => new ProgressDialogV(context, (IProgressDialog)dataContext),
      _ => null
    };
  }
}