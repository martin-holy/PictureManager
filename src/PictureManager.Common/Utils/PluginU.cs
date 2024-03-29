using MH.Utils;
using PictureManager.Plugins.Common.Interfaces;
using System;
using System.IO;

namespace PictureManager.Common.Utils;

public static class PluginU {
  public static IPluginCore GetPluginCore(string pluginName) {
    try {
      var pluginPath = Path.Combine("plugins", pluginName, $"{pluginName}.Common.dll");
      return Plugin.LoadPlugin<IPluginCore>(pluginPath);
    }
    catch (Exception ex) {
      Log.Error(ex);
      return null;
    }
  }
}