using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.Extensions;
using PictureManager.Common.Features.Folder;

namespace PictureManager.Common.Features.MediaItem.Video;

public class VideoItemM : MediaItemM, IVideoItem {
  private int _timeStart;

  public VideoM Video { get; set; }
  public int TimeStart { get => _timeStart; set { _timeStart = value; OnPropertyChanged(); } }

  public VideoItemM(int id, VideoM video, int timeStart) : base(id) {
    _timeStart = timeStart;
    Video = video;
  }

  public override FolderM Folder { get => Video.Folder; set => Video.Folder = value; }
  public override string FileName { get => Video.FileName; set => Video.FileName = value; }
  public override string FilePath => Video.FilePath;
  public override int Width { get => Video.Width; set => Video.Width = value; }
  public override int Height { get => Video.Height; set => Video.Height = value; }
  public override Orientation Orientation { get => Video.Orientation; set => Video.Orientation = value; }
  public override int ThumbWidth { get => Video.ThumbWidth; set => Video.ThumbWidth = value; }
  public override int ThumbHeight { get => Video.ThumbHeight; set => Video.ThumbHeight = value; }

  public override string FilePathCache =>
    IOExtensions.PathCombine(Video.Folder.FullPathCache, FileNameCache(Video.FileName));
}