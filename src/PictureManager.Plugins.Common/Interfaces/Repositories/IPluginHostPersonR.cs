using MH.Utils.Interfaces;
using PictureManager.Plugins.Common.Interfaces.Models;
using System.Collections.Generic;

namespace PictureManager.Plugins.Common.Interfaces.Repositories;

public interface IPluginHostPersonR {
  public List<IPluginHostPersonM> Link(string csv, IDataAdapter seeker);
}