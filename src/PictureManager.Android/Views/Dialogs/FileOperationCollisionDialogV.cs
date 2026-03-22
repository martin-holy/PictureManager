using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Content;
using MH.UI.Android.Binding;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.Utils;
using MH.Utils.Disposables;
using PictureManager.Android.ViewModels;
using PictureManager.Common.Features.Common;
using System;
using System.Threading;

namespace PictureManager.Android.Views.Dialogs;

public sealed class FileOperationCollisionDialogV : LinearLayout {
  private readonly ThumbLayout _srcThumb;
  private readonly ThumbLayout _dstThumb;

  public FileOperationCollisionDialogV(Context context, FileOperationCollisionDialog dataContext, BindingScope bindings) : base(context) {
    Orientation = Orientation.Vertical;
    LayoutParameters = LPU.LinearMatchWrap();

    _srcThumb = new ThumbLayout(context);
    _dstThumb = new ThumbLayout(context);

    _srcThumb.Bind(dataContext.SrcFile);

    dataContext.Bind(nameof(FileOperationCollisionDialog.DstFile), x => x.DstFile, x => { _dstThumb.Bind(x); _updateComparison(); });

    var fileName = new EditText(context)
      .BindText(dataContext, nameof(FileOperationCollisionDialog.FileName), x => x.FileName, (s, p) => s.FileName = p, bindings);

    var thumbs = LayoutU.Horizontal(context)
      .Add(_srcThumb, LPU.Linear(0, LPU.Wrap, 1f))
      .Add(_dstThumb, LPU.Linear(0, LPU.Wrap, 1f));

    AddView(thumbs, LPU.LinearMatchWrap());
    AddView(fileName, LPU.LinearMatchWrap().WithMargin(DimensU.Spacing));

    Post(_setEqualHeight);
  }

  private void _setEqualHeight() {
    int width = _srcThumb.Width; // both are equal

    float srcRatio = _srcThumb.AspectRatio ?? 1f;
    float dstRatio = _dstThumb.AspectRatio ?? 1f;

    int srcHeight = (int)(width * srcRatio);
    int dstHeight = (int)(width * dstRatio);

    int finalHeight = Math.Max(srcHeight, dstHeight);

    _srcThumb.SetImageHeight(finalHeight);
    _dstThumb.SetImageHeight(finalHeight);
  }

  private void _updateComparison() {
    bool dimensDiff = _srcThumb.Dimens.Text != _dstThumb.Dimens.Text;
    bool sizeDiff = _srcThumb.Size.Text != _dstThumb.Size.Text;
    bool dateDiff = _srcThumb.LastWrite.Text != _dstThumb.LastWrite.Text;

    _srcThumb.SetComparisonState(dimensDiff, sizeDiff, dateDiff);
    _dstThumb.SetComparisonState(dimensDiff, sizeDiff, dateDiff);
  }

  private sealed class ThumbLayout : LinearLayout {
    private CancellationTokenSource? _cts;
    private readonly ImageView _thumb;
    public TextView Dimens { get; }
    public TextView Size { get; }
    public TextView LastWrite { get; }

    public float? AspectRatio { get; private set; }

    public ThumbLayout(Context? context) : base(context) {
      Orientation = Orientation.Vertical;
      _thumb = new(context);
      _thumb.SetScaleType(ImageView.ScaleType.FitCenter);
      Dimens = new(context);
      Size = new(context);
      LastWrite = new(context);

      AddView(_thumb, LPU.LinearMatchWrap().WithMargin(DimensU.Spacing));
      AddView(Dimens, LPU.LinearWrap(GravityFlags.CenterHorizontal));
      AddView(Size, LPU.LinearWrap(GravityFlags.CenterHorizontal));
      AddView(LastWrite, LPU.LinearWrap(GravityFlags.CenterHorizontal));
    }

    public void Bind(FileCollisionInfo? info) {
      Unbind();
      if (info == null) return;
      Size.Text = info.Size;
      LastWrite.Text = info.LastWrite;

      if (info.MediaItem == null) return;
      Dimens.Text = $"{info.MediaItem.Width}x{info.MediaItem.Height}";
      AspectRatio = (float)info.MediaItem.Height / info.MediaItem.Width;

      _cts = new CancellationTokenSource();
      _ = MediaItemVM.LoadThumbnailAsync(info.MediaItem, _thumb, Context!, _cts.Token);
    }

    public void SetImageHeight(int height) {
      var lp = _thumb.LayoutParameters!;
      lp.Height = height;
      _thumb.LayoutParameters = lp;
    }

    public void SetComparisonState(bool dimensDiff, bool sizeDiff, bool dateDiff) {
      Dimens.SetTextColor(_getComparisonColor(dimensDiff));
      Size.SetTextColor(_getComparisonColor(sizeDiff));
      LastWrite.SetTextColor(_getComparisonColor(dateDiff));
    }

    private global::Android.Graphics.Color _getComparisonColor(bool different) =>
      new(ContextCompat.GetColor(Context, different ? Resource.Color.c_white : Resource.Color.gray4));

    public void Unbind() {
      _cts?.Cancel();
      _cts?.Dispose();
      _cts = null;
      _thumb.SetImageBitmap(null);
      Dimens.Text = string.Empty;
      Size.Text = string.Empty;
      LastWrite.Text = string.Empty;
      AspectRatio = null;
    }

    protected override void Dispose(bool disposing) {
      if (disposing) Unbind();
      base.Dispose(disposing);
    }
  }
}