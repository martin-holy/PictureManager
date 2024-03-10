namespace MH.UI.Interfaces;

public interface IVideoClip : IVideoItem {
  public int TimeEnd { get; set; }
  public double Volume { get; set; }
  public double Speed { get; set; }
}