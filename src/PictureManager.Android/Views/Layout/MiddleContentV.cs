using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Utils;
using MH.Utils;
using MH.Utils.Disposables;
using PictureManager.Android.Views.Sections;
using PictureManager.Common;
using PictureManager.Common.Layout;

namespace PictureManager.Android.Views.Layout;

public class MiddleContentV : FrameLayout {
  private readonly MainTabsV _mainTabs;
  private readonly MediaViewerV _mediaViewer;

  public MiddleContentV(Context context, CoreVM coreVM, BindingScope bindings) : base(context) {
    _mainTabs = new(context, coreVM.MainTabs);
    _mediaViewer = new(context, coreVM.MediaViewer, bindings);

    AddView(_mainTabs, LPU.FrameMatch());
    AddView(_mediaViewer, LPU.FrameMatch());

    coreVM.MainWindow.Bind(nameof(MainWindowVM.IsInViewMode), x => x.IsInViewMode, _updateVisibility);
  }

  private void _updateVisibility(bool isInViewMode) {
    _mainTabs.Visibility = isInViewMode ? ViewStates.Gone : ViewStates.Visible;
    _mediaViewer.Visibility = isInViewMode ? ViewStates.Visible : ViewStates.Gone;
  }
}