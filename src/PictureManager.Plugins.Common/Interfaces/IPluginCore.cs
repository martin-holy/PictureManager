using PictureManager.Plugins.Common.Interfaces.Repositories;
using PictureManager.Plugins.Common.Interfaces.ViewModels;
using System;
using System.Threading.Tasks;

namespace PictureManager.Plugins.Common.Interfaces;

public interface IPluginCore {
  public IPluginCoreVM VM { get; }
  public Task InitAsync(IPluginHostCoreR pmCoreR, IProgress<string> progress);
  public void AfterInit(IPluginHostCoreVM phCoreVM);
}