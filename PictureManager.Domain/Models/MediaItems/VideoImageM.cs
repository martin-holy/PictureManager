using MH.UI.Interfaces;

namespace PictureManager.Domain.Models.MediaItems;

public sealed class VideoImageM : VideoItemM, IVideoImage {
  public override string FilePathCache => GetFilePathCache();

  public VideoImageM(int id, VideoM video) : base(id, video) { }

  private string GetFilePathCache() {
    var fpc = Video.FilePathCache;
    return $"{fpc[..fpc.LastIndexOf('.')]}_image_{GetHashCode().ToString()}.jpg";
  }
}