using MH.Utils.Interfaces;
using PictureManager.Plugins.Common.Interfaces.Models;
using System.Collections.Generic;

namespace PictureManager.Plugins.Common.Interfaces.Repositories;

public interface IPluginHostKeywordR {
  public List<IPluginHostKeywordM> Link(string csv, IDataAdapter seeker);
}