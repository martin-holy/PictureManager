using MH.Utils.Interfaces;
using PictureManager.Plugins.Common.Interfaces.Models;
using System.Collections.Generic;

namespace PictureManager.Plugins.Common.Interfaces.Repositories;

public interface IPluginHostKeywordR : IPluginHostR<IPluginHostKeywordM> {
  public IPluginHostKeywordM GetByFullPath(string fullPath, IEnumerable<ITreeItem> src = null, ITreeItem rootForNew = null);
  public ITreeItem GetCategoryGroup(string name);
}