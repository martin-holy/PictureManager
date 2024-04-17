namespace PictureManager.Interfaces.Models;

public interface IMediaItemM {
  public int Id { get; }
  public int ThumbWidth { get; }
  public int ThumbHeight { get; }
  public void SetThumbSize(bool reload = false);
}