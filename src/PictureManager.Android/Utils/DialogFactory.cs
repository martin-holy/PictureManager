using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Controls;
using System;
using System.Collections.Generic;

namespace PictureManager.Android.Utils;

public static class DialogFactory {
  private static readonly Dictionary<Type, (Dialog dialogVM, View dialogV)> _dialogs = [];

  public static View? GetDialogView(Context context, Dialog dialog) {
    var layout = new LinearLayout(context) {
      Orientation = Orientation.Vertical,
      LayoutParameters = new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
    };

    var textView = new TextView(context) {
      Text = "This is dialog content",
      Gravity = GravityFlags.Center
    };
    layout.AddView(textView);

    return layout;
  }
}