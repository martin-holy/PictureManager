using System;
using System.Threading.Tasks;

namespace PictureManager.Common.Interfaces.Plugin;

public interface IPluginCore {
  public string Name { get; }
  public IPluginCoreVM VM { get; }
  public Task InitAsync(Core pmCore, CoreR pmCoreR, IProgress<string> progress);
  public void AfterInit(CoreS pmCoreS, CoreVM pmCoreVM);
}