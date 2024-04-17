using MH.Utils.BaseClasses;
using System.Collections.Generic;

namespace PictureManager.Interfaces.Plugin;

public interface IPluginCoreVM {
  public string PluginIcon { get; }
  public string PluginTitle { get; }
  public List<RelayCommand> MainMenuCommands { get; }
}