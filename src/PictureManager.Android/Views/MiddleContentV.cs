using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.ViewPager2.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Views;
using MH.UI.ViewModels;
using MH.Utils.Extensions;
using PictureManager.Android.Views.Sections;
using PictureManager.Common;
using PictureManager.Common.Features.MediaItem;
using System.ComponentModel;

namespace PictureManager.Android.Views;

public class MiddleContentV : LinearLayout {
  private readonly CoreVM _coreVM;
  private readonly ViewPager2 _mainViewPager;

  private bool _disposed;
  private readonly TabControlHost _mainTabs;
  private readonly MediaViewerV _mediaViewer;

  public MiddleContentV(Context context, CoreVM coreVM, ViewPager2 mainViewPager) : base(context) {
    _coreVM = coreVM;
    _mainViewPager = mainViewPager;

    _mainTabs = new(context, coreVM.MainTabs, _getMainTabsView) {
      LayoutParameters = new LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
    };
    AddView(_mainTabs);

    _mediaViewer = new(context, coreVM.MediaViewer) {
      LayoutParameters = new LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
    };
    AddView(_mediaViewer);

    // TODO remove test data
    coreVM.MainTabs.Add("IconFolder", "Test", new TextView(Context) { Text = "Test Panel Folder" });
    coreVM.MainTabs.Add("IconTag", "Test Tag", new TextView(Context) { Text = "Test Panel Tag" });

    _updateVisibility(coreVM.MediaViewer.IsVisible);

    coreVM.MediaViewer.PropertyChanged += _onMediaViewerPropertyChanged;
  }

  protected override void Dispose(bool disposing) {
    if (_disposed) return;
    if (disposing) {
      _coreVM.MediaViewer.PropertyChanged -= _onMediaViewerPropertyChanged;
      _mainTabs.Dispose();
      _mediaViewer.Dispose();
    }
    _disposed = true;
    base.Dispose(disposing);
  }

  private void _onMediaViewerPropertyChanged(object? sender, PropertyChangedEventArgs e) {
    if (e.Is(nameof(MediaViewerVM.IsVisible)))
      _updateVisibility(_coreVM.MediaViewer.IsVisible);
    else if (e.Is(nameof(MediaViewerVM.UserInputMode)))
      _mainViewPager.UserInputEnabled = _coreVM.MediaViewer.UserInputMode == MediaViewerVM.UserInputModes.Disabled;
  }

  private View? _getMainTabsView(LinearLayout container, object? item) =>
    item switch {
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