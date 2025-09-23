using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Utils;
using PictureManager.Common;
using PictureManager.Common.Layout;
using System.Collections.Generic;

namespace PictureManager.Android.Views.Layout;

public class ToolBarV : LinearLayout {
  private readonly List<CommandBinding> _commandBindings = [];
  private bool _disposed;

  public ToolBarV(Context context, MainWindowVM mainWindowVM) : base(context) {
    Orientation = Orientation.Horizontal;
    AddView(new ButtonMenu(Context!, mainWindowVM.MainMenu, mainWindowVM.MainMenu.Icon));

    var logBtn = new IconButton(context);
    logBtn.SetImageDrawable(Icons.GetIcon(context, CoreVM.OpenLogCommand.Icon));
    _commandBindings.Add(new(logBtn, CoreVM.OpenLogCommand));

    BindingU.Bind(Core.VM.Log.Items, x => x.Count, count => {
      logBtn.Visibility = count > 0 ? ViewStates.Visible : ViewStates.Gone;
    });

    AddView(logBtn);
  }

  protected override void Dispose(bool disposing) {
    if (_disposed) return;
    if (disposing) {
      foreach (var cb in _commandBindings) cb.Dispose();
      _commandBindings.Clear();
    }
    _disposed = true;
    base.Dispose(disposing);
  }
}