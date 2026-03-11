using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Binding;
using MH.UI.Android.Controls;
using MH.Utils;
using MH.Utils.Disposables;
using PictureManager.Common;
using PictureManager.Common.Layout;
using System.Collections;

namespace PictureManager.Android.Views.Sections.ToolBarPanels;

public sealed class CommonToolBarPanel : LinearLayout {
  public CommonToolBarPanel(Context context, MainWindowVM mainWindowVM, BindingScope bindings) : base(context) {
    Orientation = Orientation.Horizontal;

    var openLog = new CompactIconTextButton(context).WithClickCommand(CoreVM.OpenLogCommand, bindings);
    var saveDb = new CompactIconTextButton(context).WithClickCommand(CoreVM.SaveDbCommand, bindings);

    bindings.AddRange([
      Core.VM.Log.Items.Bind(nameof(ICollection.Count), x => x.Count, count => _bindButton(openLog, count)),
      Core.R.Bind(nameof(CoreR.Changes), x => x.Changes, changes => _bindButton(saveDb, changes))]);

    AddView(new ButtonMenu(Context!, mainWindowVM.MainMenu, mainWindowVM.MainMenu.Icon));
    AddView(openLog);
    AddView(saveDb);
  }

  private static void _bindButton(CompactIconTextButton btn, int value) {
    btn.Visibility = value > 0 ? ViewStates.Visible : ViewStates.Gone;
    btn.Text.Text = value.ToString();
  }
}