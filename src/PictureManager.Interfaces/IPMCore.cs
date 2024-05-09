using PictureManager.Interfaces.Settings;

namespace PictureManager.Interfaces;

public interface IPMCore {
  IPMSettings Settings { get; }
}