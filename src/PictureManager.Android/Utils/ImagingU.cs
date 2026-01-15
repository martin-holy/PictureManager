using Android.Graphics;
using MH.UI.Android.Extensions;
using PictureManager.Common;
using PictureManager.Common.Features.Segment;

namespace PictureManager.Android.Utils;

public static class ImagingU {
  public static void ExportSegment(SegmentM segment, string dest) {
    var x = (int)segment.X;
    var y = (int)segment.Y;
    var size = (int)segment.Size;
    var rect = new Rect(x, y, x + size, y + size);
    using var bmp = BitmapExtensions.Create(segment.MediaItem.FilePath, rect);
    bmp?.SaveAsJpeg(dest, Core.Settings.Common.JpegQuality);
  }
}
