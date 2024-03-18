using PictureManager.Plugins.Common.Interfaces.Repositories;
using System.Threading.Tasks;
using System;

namespace PictureManager.Plugins.Common.Interfaces;

public interface IPluginCore {
  public Task InitAsync(IPluginHostCoreR pmCoreR, IProgress<string> progress);
  public void AfterInit();
}