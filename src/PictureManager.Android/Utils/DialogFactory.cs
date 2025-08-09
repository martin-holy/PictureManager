using Android.Content;
using Android.Views;
using MH.UI.Controls;

namespace PictureManager.Android.Utils;

public static class DialogFactory {

  public static View? GetDialog(Context context, Dialog dataContext) {
    // TODO add app dialogs
    return dataContext switch {
      _ => null
    };
  }
}