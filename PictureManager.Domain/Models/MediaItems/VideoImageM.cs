using MH.UI.Interfaces;

namespace PictureManager.Domain.Models.MediaItems;

public sealed class VideoImageM(int id, VideoM video, int timeStart) : VideoItemM(id, video, timeStart), IVideoImage {
  public override string FilePathCache => GetFilePathCache();

  private string GetFilePathCache() {
    var fpc = Video.FilePathCache;
    return $"{fpc[..fpc.LastIndexOf('.')]}_image_{GetHashCode().ToString()}.jpg";
  }
}