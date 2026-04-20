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

  public MainTabsV MainTabs { get; }
  public MediaViewerV MediaViewer { get; }

  public MiddleContentV(Context context, CoreVM coreVM, BindingScope bindings) : base(context) {
    _coreVM = coreVM;

    MainTabs = new(context, coreVM.MainTabs);
    MediaViewer = new(context, coreVM.MediaViewer, bindings);

    AddView(MainTabs, LPU.FrameMatch());
    AddView(MediaViewer, LPU.FrameMatch());

    coreVM.MainWindow.Bind(nameof(MainWindowVM.IsInViewMode), x => x.IsInViewMode, _onMainWindowIsInViewModeChanged);
  }

  private void _onMainWindowIsInViewModeChanged(bool isInViewMode) {
    if (isInViewMode) {
      MainTabs.Visibility = ViewStates.Gone;
      MediaViewer.Visibility = ViewStates.Visible;
    } else {
      _coreVM.Video.MediaPlayer.IsPlaying = false;
      MediaViewer.Visibility = ViewStates.Gone;
      MainTabs.Visibility = ViewStates.Visible;
    }
  }
}