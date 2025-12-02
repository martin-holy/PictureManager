using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Utils;
using MH.Utils;
using PictureManager.Android.Views.Sections;
using PictureManager.Common;
using PictureManager.Common.Features.MediaItem;

namespace PictureManager.Android.Views.Layout;

public class MiddleContentV : LinearLayout {
  private readonly MainTabsV _mainTabs;
  private readonly MediaViewerV _mediaViewer;

  public MiddleContentV(Context context, CoreVM coreVM) : base(context) {
    _mainTabs = new(context, coreVM.MainTabs);
    _mediaViewer = new(context, coreVM.MediaViewer);

    AddView(_mainTabs, new LayoutParams(LPU.Match, LPU.Match));
    AddView(_mediaViewer, new LayoutParams(LPU.Match, LPU.Match));

    this.Bind(coreVM.MediaViewer, nameof(MediaViewerVM.IsVisible), x => x.IsVisible, (t, p) => t._updateVisibility(p));
  }

  private void _updateVisibility(bool viewerIsVisible) {
    _mainTabs.Visibility = viewerIsVisible ? ViewStates.Gone : ViewStates.Visible;
    _mediaViewer.Visibility = viewerIsVisible ? ViewStates.Visible : ViewStates.Gone;
  }
}