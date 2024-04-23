using MH.Utils;
using MovieManager.Common.Repositories;
using MovieManager.Common.Services;
using MovieManager.Common.ViewModels;
using MovieManager.Plugins.Common.Interfaces;
using PictureManager.Interfaces;
using PictureManager.Interfaces.Plugin;
using PictureManager.Interfaces.Repositories;
using PictureManager.Interfaces.Services;
using PictureManager.Interfaces.ViewModels;
using System;
using System.IO;
using System.Threading.Tasks;
using IMMPluginCore = MovieManager.Plugins.Common.Interfaces.IPluginCore;
using IPMPluginCore = PictureManager.Interfaces.Plugin.IPluginCore;

namespace MovieManager.Common;

public sealed class Core : IPMPluginCore {
  public string Name => "MovieManager";
  public string BaseDir { get; }
  public string PluginsDir { get; }
  public static ICore PMCore { get; private set; }
  public static Core Inst { get; private set; }
  public static CoreR R { get; private set; }
  public static CoreS S { get; private set; }
  public static CoreVM VM { get; private set; }
  public static IMovieSearchPlugin MovieSearch { get; private set; }
  public static IMovieDetailPlugin MovieDetail { get; private set; }

  IPluginCoreVM IPMPluginCore.VM => VM;

  public Core() {
    Inst = this;
    BaseDir = Path.Combine("plugins", Name);
    PluginsDir = Path.Combine(BaseDir, "plugins");
  }

  public Task InitAsync(ICore pmCore, ICoreR phCoreR, IProgress<string> progress) {
    PMCore = pmCore;
    R = new(phCoreR, this);

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

  public void AfterInit(ICoreS phCoreS, ICoreVM phCoreVM) {
    S = new(phCoreS, R);
    VM = new(phCoreVM, S, R);
    AttachEvents();
  }

  private Task LoadPlugins(IProgress<string> progress) {
    SetMovieSearchPlugin();
    SetMovieDetailPlugin();
    return Task.CompletedTask;
  }

  private void AttachEvents() {

  }

  private void SetMovieSearchPlugin() {
    //var path = Path.Combine(PluginsDir, "MovieManager.Plugins.IMDbCom.dll");
    var path = Path.Combine(PluginsDir, "MovieManager.Plugins.CSFDcz.dll");
    if (Plugin.LoadPlugin<IMMPluginCore>(path) is not { } pc) return;
    MovieSearch = pc as IMovieSearchPlugin;
  }

  private void SetMovieDetailPlugin() {
    var path = Path.Combine(PluginsDir, "MovieManager.Plugins.IMDbCom.dll");
    if (Plugin.LoadPlugin<IMMPluginCore>(path) is not { } pc) return;
    MovieDetail = pc as IMovieDetailPlugin;
  }
}