using PictureManager.Interfaces.Repositories;
using PictureManager.Interfaces.Services;
using PictureManager.Interfaces.ViewModels;
using System;
using System.Threading.Tasks;

namespace PictureManager.Interfaces.Plugin;

public interface IPluginCore {
  public string Name { get; }
  public IPluginCoreVM VM { get; }
  public Task InitAsync(IPMCore pmCore, IPMCoreR pmCoreR, IProgress<string> progress);
  public void AfterInit(IPMCoreS pmCoreS, IPMCoreVM pmCoreVM);
}