using Android.Content;
using Android.Views;
using MH.UI.Android.Dialogs;
using MH.UI.Controls;
using MH.UI.Dialogs;
using PictureManager.Common.Features.GeoLocation;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.MediaItem.Image;

namespace PictureManager.Android.Utils;

public static class DialogFactory {
  public static View? GetDialog(Context context, Dialog dataContext) {
    return dataContext switch {
      GetGeoNamesFromWebDialog => new ProgressDialogV(context, (IProgressDialog)dataContext),
      ReadGeoLocationFromFilesDialog => new ProgressDialogV(context, (IProgressDialog)dataContext),
      ComputeImageHashesDialog => new ProgressDialogV(context, (IProgressDialog)dataContext),
      ReloadMetadataDialog => new ProgressDialogV(context, (IProgressDialog)dataContext),
      SaveMetadataDialog => new ProgressDialogV(context, (IProgressDialog)dataContext),
      _ => null
    };
  }
}