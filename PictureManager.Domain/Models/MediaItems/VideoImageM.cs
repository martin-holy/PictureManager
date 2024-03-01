using MH.UI.Interfaces;
using MH.Utils.Extensions;

namespace PictureManager.Domain.Models.MediaItems;

public sealed class VideoImageM(int id, VideoM video, int timeStart) : VideoItemM(id, video, timeStart), IVideoImage {
  public override string FilePathCache =>
    IOExtensions.PathCombine(Video.Folder.FullPathCache, FileNameCache(Video.FileName));

  public override string FileNameCache(string fileName) =>
    $"{fileName[..fileName.LastIndexOf('.')]}_image_{GetHashCode().ToString()}.jpg";
}