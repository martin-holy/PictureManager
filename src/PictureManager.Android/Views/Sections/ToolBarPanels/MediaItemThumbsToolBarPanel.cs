using Android.Content;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Utils;
using MH.Utils;
using PictureManager.Common;
using PictureManager.Common.Features.MediaItem;
using System;

namespace PictureManager.Android.Views.Sections.ToolBarPanels;

public sealed class MediaItemThumbsToolBarPanel : LinearLayout {
  public MediaItemThumbsToolBarPanel(Context context, MediaItemsViewsVM mediaItemViewsVM) : base(context) {
    Orientation = Orientation.Horizontal;

    AddView(ThumbScalePopupSlider.Create(context, mediaItemViewsVM));
  }

  private class ThumbScalePopupSlider {
    private readonly MediaItemsViewsVM _mediaItemViewsVM;
    private readonly PopupSlider _slider;
    private readonly CompactIconTextButton _button;
    IDisposable? _sliderBinding;
    IDisposable? _buttonBinding;

    private ThumbScalePopupSlider(Context context, MediaItemsViewsVM mediaItemViewsVM) {
      _mediaItemViewsVM = mediaItemViewsVM;

      _button = new CompactIconTextButton(context);
      _button.Icon.Bind(Res.IconMagnify);
      _slider = new PopupSlider(context, 0.3, 1, 0.1, _button);
      _slider.Slider.StopTrackingTouch += (_, _) => _mediaItemViewsVM.Current?.ReWrapAll();

      this.Bind(mediaItemViewsVM, nameof(MediaItemsViewsVM.Current), x => x.Current, _onCurrentViewChanged);
    }

    public static PopupSlider Create(Context context, MediaItemsViewsVM mediaItemViewsVM) =>
      new ThumbScalePopupSlider(context, mediaItemViewsVM)._slider;

    private static void _onCurrentViewChanged(ThumbScalePopupSlider self, MediaItemsViewVM? view) {
      self._sliderBinding?.Dispose();
      self._buttonBinding?.Dispose();
      if (view == null) return;
      self._slider.Slider.BindProgress(view, nameof(MediaItemCollectionView.ThumbScale),
        x => x.ThumbScale, (s, p) => s.ThumbScale = p, out self._sliderBinding);
      self._button.Text.BindText(view, nameof(MediaItemCollectionView.ThumbScale),
        x => x.ThumbScale, x => x.ToString("G2"), out self._buttonBinding);
    }
  }
}