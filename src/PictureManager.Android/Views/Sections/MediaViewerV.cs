using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.ViewPager2.Widget;
using MH.UI.Android.Controls.Recycler;
using MH.UI.Android.Utils;
using MH.Utils;
using MH.Utils.Disposables;
using PictureManager.Android.Views.Entities;
using PictureManager.Common;
using PictureManager.Common.Features.MediaItem;
using System.Collections.Generic;

namespace PictureManager.Android.Views.Sections;

public class MediaViewerV : FrameLayout {
  private readonly ViewPager2 _viewPager;
  private readonly BindableAdapter<MediaItemM> _adapter;
  private readonly PageChangeCallback _pageChangeCallback;
  private bool _disposed;

  public MediaViewerVM DataContext { get; }
  public static Java.Lang.Object DeactivatedPagePayload { get; } = new Java.Lang.String("DeactivatedPage");

  public MediaViewerV(Context context, MediaViewerVM dataContext, BindingScope bindings) : base(context) {
    DataContext = dataContext;
    _adapter = new(
      () => dataContext.MediaItems,
      ctx => new MediaItemFullV(ctx, dataContext, Core.VM.Segment.Rect, new(Core.S.Segment) { EditLimit = 20 }),
      () => new(LPU.Match, LPU.Match),
      null,
      _onPayloadBind);

    SetBackgroundResource(Resource.Color.c_static_ba);

    _pageChangeCallback = new PageChangeCallback(_adapter, dataContext);
    _viewPager = new(context) { Adapter = _adapter };
    _viewPager.RegisterOnPageChangeCallback(_pageChangeCallback);
    AddView(_viewPager, LPU.FrameMatch());

    bindings.AddRange([
      dataContext.Bind(nameof(MediaViewerVM.MediaItems), x => x.MediaItems, x => {
        _adapter.NotifyDataSetChanged();
        if (x!.Count > 0)
          _viewPager.SetCurrentItem(DataContext.IndexOfCurrent, false);
      }),

      dataContext.Bind(nameof(MediaViewerVM.UserInputMode), x => x.UserInputMode, x =>
        _viewPager.UserInputEnabled = x == MediaViewerVM.UserInputModes.Browse)
    ]);
  }

  private bool _onPayloadBind(View view, int position, IList<Java.Lang.Object> payloads) {
    foreach (var payload in payloads)
      if (Equals(payload, DeactivatedPagePayload) && view is MediaItemFullV miView) {
        miView.ResetForInactivePage();
        return true;
      }

    return false;
  }

  protected override void Dispose(bool disposing) {
    if (_disposed) return;
    if (disposing) {
      _viewPager.UnregisterOnPageChangeCallback(_pageChangeCallback);
      _viewPager.Adapter = null;
      _adapter.Dispose();
      _pageChangeCallback.Dispose();
    }
    _disposed = true;
    base.Dispose(disposing);
  }

  private class PageChangeCallback(BindableAdapter<MediaItemM> adapter, MediaViewerVM mediaViewerVM) : ViewPager2.OnPageChangeCallback {
    private int _lastPosition = -1;

    public override void OnPageSelected(int position) {
      if (_lastPosition >= 0)
        adapter.NotifyItemChanged(_lastPosition, DeactivatedPagePayload);

      _lastPosition = position;

      mediaViewerVM.GoTo(position);
    }
  }
}