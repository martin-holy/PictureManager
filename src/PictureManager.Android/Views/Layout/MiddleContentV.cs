using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Utils;
using MH.UI.Android.Views;
using MH.UI.ViewModels;
using MH.Utils;
using PictureManager.Android.Views.Sections;
using PictureManager.Common;
using PictureManager.Common.Features.Common;
using PictureManager.Common.Features.MediaItem;

namespace PictureManager.Android.Views.Layout;

public class MiddleContentV : LinearLayout {
  private readonly TabControlHost _mainTabs;
  private readonly MediaViewerV _mediaViewer;

  public MiddleContentV(Context context, CoreVM coreVM) : base(context) {
    _mainTabs = new(context, coreVM.MainTabs, _getMainTabsView);
    _mediaViewer = new(context, coreVM.MediaViewer);

    AddView(_mainTabs, new LayoutParams(LPU.Match, LPU.Match));
    AddView(_mediaViewer, new LayoutParams(LPU.Match, LPU.Match));

    this.Bind(coreVM.MediaViewer, x => x.IsVisible, (t, p) => t._updateVisibility(p));
  }

  private View? _getMainTabsView(LinearLayout container, object? item) =>
    item switch {
      AllSettings allSettings => new SettingsV(container.Context!, allSettings),
      MediaItemsViewVM miv => new MediaItemsViewV(container.Context!, miv),
      LogVM log => new LogV(container.Context!, log),
      View view => view,
      _ => null
    };

  private void _updateVisibility(bool viewerIsVisible) {
    _mainTabs.Visibility = viewerIsVisible ? ViewStates.Gone : ViewStates.Visible;
    _mediaViewer.Visibility = viewerIsVisible ? ViewStates.Visible : ViewStates.Gone;
  }
}