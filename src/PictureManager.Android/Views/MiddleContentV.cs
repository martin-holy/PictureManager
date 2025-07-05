using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.ViewPager2.Widget;
using MH.UI.Android.Controls;
using MH.Utils.Extensions;
using PictureManager.Android.Views.Sections;
using PictureManager.Common;
using PictureManager.Common.Features.MediaItem;

namespace PictureManager.Android.Views;

public class MiddleContentV : LinearLayout {
  private TabControlHost _mainTabs = null!;
  private MediaViewerV _mediaViewer = null!;
  private ViewPager2? _mainViewPager;

  public MiddleContentV(Context context) : base(context) => _initialize(context);
  public MiddleContentV(Context context, IAttributeSet attrs) : base(context, attrs) => _initialize(context);
  protected MiddleContentV(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) => _initialize(Context!);

  private void _initialize(Context context) {
    _mainTabs = new(context) {
      LayoutParameters = new LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent),
      GetItemView = _getMainTabsView
    };
    AddView(_mainTabs);

    _mediaViewer = new(context) {
      LayoutParameters = new LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
    };
    AddView(_mediaViewer);
  }

  public void Bind(CoreVM coreVM, ViewPager2 mainViewPager) {
    _mainViewPager = mainViewPager;
    // TODO remove test data
    coreVM.MainTabs.Add("IconFolder", "Test", new TextView(Context) { Text = "Test Panel Folder" });
    coreVM.MainTabs.Add("IconTag", "Test Tag", new TextView(Context) { Text = "Test Panel Tag" });

    _updateVisibility(coreVM.MediaViewer.IsVisible);
    _mainTabs.Bind(coreVM.MainTabs);
    _mediaViewer.Bind(coreVM.MediaViewer);

    coreVM.MediaViewer.PropertyChanged += (_, e) => {
      if (e.Is(nameof(MediaViewerVM.IsVisible)))
        _updateVisibility(coreVM.MediaViewer.IsVisible);
      else if (e.Is(nameof(MediaViewerVM.IsSwipeEnabled)))
        _mainViewPager.UserInputEnabled = !coreVM.MediaViewer.IsSwipeEnabled;
    };
  }

  private View? _getMainTabsView(LinearLayout container, object? item) =>
    item switch {
      MediaItemsViewVM miv => new MediaItemsViewV(container.Context!).Bind(miv),
      View view => view,
      _ => null
    };

  private void _updateVisibility(bool viewerIsVisible) {
    _mainTabs.Visibility = viewerIsVisible ? ViewStates.Gone : ViewStates.Visible;
    _mediaViewer.Visibility = viewerIsVisible ? ViewStates.Visible : ViewStates.Gone;
  }
}