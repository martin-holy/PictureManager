using MH.UI.Dialogs;
using MH.Utils.Extensions;
using System.IO;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.MediaItem.Image;

public sealed class CompressDialog : ParallelProgressDialog<ImageM> {
  private readonly ImageS _imageS;
  private int _jpegQualityLevel;
  private long _totalSourceSize;
  private long _totalCompressedSize;

  public int JpegQualityLevel { get => _jpegQualityLevel; set { _jpegQualityLevel = value; OnPropertyChanged(); } }
  public string TotalSourceSize => IOExtensions.FileSizeToString(_totalSourceSize);
  public string TotalCompressedSize => IOExtensions.FileSizeToString(_totalCompressedSize);

  public CompressDialog(ImageM[] items, ImageS imageS, int quality) :
    base("Compress images", MH.UI.Res.IconImage, items, MH.UI.Res.IconImage, "Compress", false) {
    _imageS = imageS;
    _jpegQualityLevel = quality;
  }

  protected override void CustomProgress(object? args) {
    if (args is not (long[] and [var sourceSize, var compressedSize])) return;
    SetSize(_totalSourceSize + sourceSize, _totalCompressedSize + compressedSize);
  }

  protected override bool DoBefore() {
    SetSize(0, 0);
    return true;
  }

  private void SetSize(long source, long compressed) {
    _totalSourceSize = source;
    _totalCompressedSize = compressed;
    OnPropertyChanged(nameof(TotalSourceSize));
    OnPropertyChanged(nameof(TotalCompressedSize));
  }

  protected override Task Do(ImageM image) {
    var sourceSize = new FileInfo(image.FilePath).Length;
    var bSuccess = _imageS.TryWriteMetadata(image, _jpegQualityLevel);
    var compressedSize = bSuccess ? new FileInfo(image.FilePath).Length : sourceSize;

    ReportProgress(image.FileName, new[] { sourceSize, compressedSize });

    return Task.CompletedTask;
  }
}