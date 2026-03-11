using Android.Content;
using Android.Widget;
using MH.UI.Android.Binding;
using MH.UI.Android.Controls;
using MH.Utils;
using MH.Utils.Disposables;
using PictureManager.Common;
using PictureManager.Common.Features.MediaItem;
using System;

namespace PictureManager.Android.Views.Sections.ToolBarPanels;

public sealed class MediaItemThumbsToolBarPanel : LinearLayout {
  public MediaItemThumbsToolBarPanel(Context context, MediaItemsViewsVM mediaItemViewsVM, BindingScope bindings) : base(context) {
    Orientation = Orientation.Horizontal;
    var scale = new ThumbScalePopupSlider(context, mediaItemViewsVM, bindings);
    scale.DisposeWith(bindings);
    AddView(scale.Slider);
  }

  private class ThumbScalePopupSlider : IDisposable {
    private readonly BindingScope _currentViewBindings = new();
    private readonly CompactIconTextButton _button;

    public PopupSlider Slider { get; }

    public ThumbScalePopupSlider(Context context, MediaItemsViewsVM mediaItemViewsVM, BindingScope bindings) {
      _button = new CompactIconTextButton(context);
      _button.Icon.Bind(Res.IconMagnify);
      Slider = new PopupSlider(context, 0.3, 1, 0.1, _button);
      Slider.Slider.StopTrackingTouch += (_, _) => mediaItemViewsVM.Current?.ReWrapAll();

      mediaItemViewsVM.Bind(nameof(MediaItemsViewsVM.Current), x => x.Current, _onCurrentViewChanged).DisposeWith(bindings);
    }

    private void _onCurrentViewChanged(MediaItemsViewVM? view) {
      _currentViewBindings.Dispose();
      if (view == null) return;
      Slider.Slider.BindProgress(view, nameof(MediaItemCollectionView.ThumbScale),
        x => x.ThumbScale, (s, p) => s.ThumbScale = p, _currentViewBindings);
      _button.Text.BindText(view, nameof(MediaItemCollectionView.ThumbScale),
        x => x.ThumbScale, x => x.ToString("G2"), _currentViewBindings);
    }

    public void Dispose() => _currentViewBindings.Dispose();
  }
}