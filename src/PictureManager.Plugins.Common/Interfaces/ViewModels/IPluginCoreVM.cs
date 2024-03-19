using MH.Utils.BaseClasses;
using System.Collections.Generic;

namespace PictureManager.Plugins.Common.Interfaces.ViewModels;

public interface IPluginCoreVM {
  public string PluginIcon { get; }
  public string PluginTitle { get; }
  public List<RelayCommand> MainMenuCommands { get; }
}