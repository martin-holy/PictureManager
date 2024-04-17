namespace PictureManager.Interfaces.Settings;

public interface IMediaItemSettings {
  public bool ScrollExactlyToMediaItem { get; }
  public double MediaItemThumbScale { get; }
  public double VideoItemThumbScale { get; }
  public int ThumbSize { get; }
}