using MH.UI.BaseClasses;
using MH.Utils.BaseClasses;
using PictureManager.Common.Interfaces.Plugin;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Common.Features.Common;

public sealed class AllSettings : ObservableObject {
  public List<UserSettings> All { get; } = [];
  public List<ListItem> Groups { get; } = [];

  public AllSettings(Settings pmSettings, List<IPluginCore> plugins) {
    Add(MH.UI.Res.IconImage, "Picture Manager", pmSettings);
    AddPlugins(plugins);
  }

  private void Add(string icon, string text, UserSettings settings) {
    All.Add(settings);
    Groups.Add(new(icon, text, settings));
    Groups.AddRange(settings.Groups);
  }

  private void AddPlugins(List<IPluginCore> plugins) {
    foreach (var plugin in plugins.Where(x => x.Settings != null))
      Add(plugin.Icon, plugin.Text, plugin.Settings!);
  }
}