using MH.Utils.Interfaces;

namespace MH.UI.Interfaces;

public interface IVideoItem : ISelectable {
  public int TimeStart { get; set; }
}