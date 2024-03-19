using MH.Utils;
using MovieManager.Common.Repositories;
using MovieManager.Common.Services;
using MovieManager.Common.ViewModels;
using PictureManager.Plugins.Common.Interfaces;
using PictureManager.Plugins.Common.Interfaces.Repositories;
using PictureManager.Plugins.Common.Interfaces.ViewModels;
using System;
using System.Threading.Tasks;

namespace MovieManager.Common;

public sealed class Core : IPluginCore {
  public static CoreR R { get; private set; }
  public static CoreS S { get; private set; }
  public static CoreVM VM { get; private set; }

  IPluginCoreVM IPluginCore.VM => VM;

  public Task InitAsync(IPluginHostCoreR phCoreR, IProgress<string> progress) {
    R = new(phCoreR);
    return Task.Run(() => {
      R.AddDataAdapters();
      progress.Report("Migrating MovieManager Database");
      R.Migrate(0, DatabaseMigration.Resolver);
      R.LoadAllTables(progress);
      R.LinkReferences(progress);
      R.ClearDataAdapters();
      R.SetIsReady();
    });
  }

  public void AfterInit() {
    S = new(R);
    VM = new(S, R);
    AttachEvents();
  }

  private void AttachEvents() {

  }
}