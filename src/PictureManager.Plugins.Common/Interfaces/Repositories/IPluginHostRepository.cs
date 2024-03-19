using MH.Utils.Interfaces;
using System.Collections.Generic;

namespace PictureManager.Plugins.Common.Interfaces.Repositories;

public interface IPluginHostRepository<T> {
  public T GetById(string id, bool nullable = false);
  public List<T> Link(string csv, IDataAdapter seeker);
}