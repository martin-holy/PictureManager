using System.Collections.Generic;
using System.Windows.Input;

namespace PictureManager.Common.Interfaces.Plugin;

public interface IPluginCoreVM {
  public string PluginIcon { get; }
  public string PluginTitle { get; }
  public List<ICommand> MainMenuCommands { get; }
}