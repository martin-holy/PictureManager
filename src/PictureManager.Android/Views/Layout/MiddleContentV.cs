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
  private readonly CoreVM _coreVM;
  private readonly MainTabsV _mainTabs;
  private readonly MediaViewerV _mediaViewer;

  public MiddleContentV(Context context, CoreVM coreVM, BindingScope bindings) : base(context) {
    _coreVM = coreVM;
    _mainTabs = new(context, coreVM.MainTabs);
    _mediaViewer = new(context, coreVM.MediaViewer, bindings);

    AddView(_mainTabs, LPU.FrameMatch());
    AddView(_mediaViewer, LPU.FrameMatch());

    coreVM.MainWindow.Bind(nameof(MainWindowVM.IsInViewMode), x => x.IsInViewMode, _onMainWindowIsInViewModeChanged);
  }

  private void _onMainWindowIsInViewModeChanged(bool isInViewMode) {
    if (isInViewMode) {
      _mainTabs.Visibility = ViewStates.Gone;
      _mediaViewer.Visibility = ViewStates.Visible;
    } else {
      _coreVM.Video.MediaPlayer.IsPlaying = false;
      _mediaViewer.Visibility = ViewStates.Gone;
      _mainTabs.Visibility = ViewStates.Visible;
    }
  }
}