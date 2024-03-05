using MH.UI.Interfaces;

namespace PictureManager.Domain.Models.MediaItems;

public sealed class VideoImageM(int id, VideoM video, int timeStart) : VideoItemM(id, video, timeStart), IVideoImage {
  public override string FileNameCache(string fileName) =>
    $"{fileName[..fileName.LastIndexOf('.')]}_image_{GetHashCode().ToString()}.jpg";
}