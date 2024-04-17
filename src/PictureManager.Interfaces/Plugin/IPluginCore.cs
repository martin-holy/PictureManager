using PictureManager.Interfaces.Repositories;
using PictureManager.Interfaces.Services;
using PictureManager.Interfaces.ViewModels;
using System;
using System.Threading.Tasks;

namespace PictureManager.Interfaces.Plugin;

public interface IPluginCore {
  public string Name { get; }
  public IPluginCoreVM VM { get; }
  public Task InitAsync(ICoreR pmCoreR, IProgress<string> progress);
  public void AfterInit(ICoreS phCoreS, ICoreVM phCoreVM);
}