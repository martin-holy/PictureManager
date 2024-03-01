using MH.UI.Interfaces;
using MH.Utils;

namespace PictureManager.Domain.Models.MediaItems;

public class VideoItemM(int id, VideoM video, int timeStart) : MediaItemM(id), IVideoItem {
  public VideoM Video { get; set; } = video;
  public int TimeStart { get => timeStart; set { timeStart = value; OnPropertyChanged(); } }

  public override FolderM Folder { get => Video.Folder; set => Video.Folder = value; }
  public override string FileName { get => Video.FileName; set => Video.FileName = value; }
  public override string FilePath => Video.FilePath;
  public override int Width { get => Video.Width; set => Video.Width = value; }
  public override int Height { get => Video.Height; set => Video.Height = value; }
  public override Orientation Orientation { get => Video.Orientation; set => Video.Orientation = value; }
  public override int ThumbWidth { get => Video.ThumbWidth; set => Video.ThumbWidth = value; }
  public override int ThumbHeight { get => Video.ThumbHeight; set => Video.ThumbHeight = value; }

  public virtual string FileNameCache(string fileName) => fileName;
}