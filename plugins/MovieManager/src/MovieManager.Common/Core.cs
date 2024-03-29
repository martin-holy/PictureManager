using MovieManager.Common.Repositories;
using MovieManager.Common.Services;
using MovieManager.Common.ViewModels;
using MovieManager.Plugins.Common.Interfaces;
using PictureManager.Plugins.Common.Interfaces.Repositories;
using PictureManager.Plugins.Common.Interfaces.ViewModels;
using System;
using System.IO;
using System.Threading.Tasks;
using IMMPluginCore = MovieManager.Plugins.Common.Interfaces.IPluginCore;
using IPMPluginCore = PictureManager.Plugins.Common.Interfaces.IPluginCore;

namespace MovieManager.Common;

public sealed class Core : IPMPluginCore {
  public string Name => "MovieManager";
  public static CoreR R { get; private set; }
  public static CoreS S { get; private set; }
  public static CoreVM VM { get; private set; }
  public static ITitleSearchPlugin TitleSearch { get; private set; }

  IPluginCoreVM IPMPluginCore.VM => VM;

  public Task InitAsync(IPluginHostCoreR phCoreR, IProgress<string> progress) {
    R = new(phCoreR);
    return Task.Run(() => {
      R.AddDataAdapters();
      progress.Report("Migrating MovieManager Database");
      R.Migrate(0, DatabaseMigration.Resolver);
      R.LoadAllTables(progress);
      R.LinkReferences(progress);
      LoadPlugins(progress).Wait();
      R.ClearDataAdapters();
      R.SetIsReady();
    });
  }

  public void AfterInit(IPluginHostCoreVM phCoreVM) {
    S = new(R);
    VM = new(phCoreVM, S, R);
    AttachEvents();
  }

  private Task LoadPlugins(IProgress<string> progress) {
    var path = Path.Combine("plugins", "MovieManager", "plugins", "MovieManager.Plugins.MediaIMDbCom.dll");
    if (MH.Utils.Plugin.LoadPlugin<IMMPluginCore>(path) is not { } pc) return Task.CompletedTask;
    TitleSearch = pc as ITitleSearchPlugin;
    return Task.CompletedTask;
  }

  private void AttachEvents() {

  }
}