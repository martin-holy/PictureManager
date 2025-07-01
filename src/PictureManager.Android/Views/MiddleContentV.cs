using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Layout;

namespace PictureManager.Android.Views;

public class MiddleContentV : LinearLayout {
  private TabControlHost _mainTabs = null!;

  public MiddleContentV(Context context) : base(context) => _initialize(context, null);
  public MiddleContentV(Context context, IAttributeSet attrs) : base(context, attrs) => _initialize(context, attrs);
  protected MiddleContentV(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) => _initialize(Context!, null);

  private void _initialize(Context context, IAttributeSet? attrs) {
    _mainTabs = new(context) { GetItemView = _getMainTabsView };
    AddView(_mainTabs);
  }

  public void Bind(MainTabsVM mainTabsVM) {
    // TODO remove test data
    mainTabsVM.Add("IconFolder", "Test", new TextView(Context) { Text = "Test Panel Folder" });
    mainTabsVM.Add("IconTag", "Test Tag", new TextView(Context) { Text = "Test Panel Tag" });

    _mainTabs.DataContext = mainTabsVM;
  }

  private View? _getMainTabsView(LinearLayout container, object? item) =>
    item switch {
      MediaItemsViewVM miv => new MediaItemsViewV(container.Context!).Bind(miv),
      View view => view,
      _ => null
    };
}