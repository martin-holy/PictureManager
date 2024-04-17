using PictureManager.Interfaces.Settings;

namespace PictureManager.Interfaces;

public interface ICore {
  ISettings Settings { get; }
}