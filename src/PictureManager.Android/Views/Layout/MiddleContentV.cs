using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Utils;
using MH.Utils;
using MH.Utils.Disposables;
using PictureManager.Android.Views.Sections;
using PictureManager.Common;
using PictureManager.Common.Features.MediaItem;

namespace PictureManager.Android.Views.Layout;

public class MiddleContentV : LinearLayout {
  private readonly MainTabsV _mainTabs;
  private readonly MediaViewerV _mediaViewer;

  public MiddleContentV(Context context, CoreVM coreVM, BindingScope bindings) : base(context) {
    _mainTabs = new(context, coreVM.MainTabs);
    _mediaViewer = new(context, coreVM.MediaViewer, bindings);

    AddView(_mainTabs, new LayoutParams(LPU.Match, LPU.Match));
    AddView(_mediaViewer, new LayoutParams(LPU.Match, LPU.Match));

    coreVM.MediaViewer.Bind(nameof(MediaViewerVM.IsVisible), x => x.IsVisible, _updateVisibility);
  }

  private void _updateVisibility(bool viewerIsVisible) {
    _mainTabs.Visibility = viewerIsVisible ? ViewStates.Gone : ViewStates.Visible;
    _mediaViewer.Visibility = viewerIsVisible ? ViewStates.Visible : ViewStates.Gone;
  }
}