using MH.UI.BaseClasses;
using MH.Utils;
using MovieManager.Common.Features.Common;
using MovieManager.Common.Utils;
using MovieManager.Plugins.Common.Interfaces;
using PictureManager.Common.Interfaces.Plugin;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PM = PictureManager.Common;

namespace MovieManager.Common;

public sealed class Core : IPluginCore {
  public string Icon => MH.UI.Res.IconMovieClapper;
  public string Text => "Movie Manager";
  public string Name => "MovieManager";
  public string BaseDir { get; }
  public string PluginsDir { get; }
  public static PM.Core PMCore { get; private set; } = null!;
  public static Core Inst { get; private set; } = null!;
  public static CoreR R { get; private set; } = null!;
  public static CoreS S { get; private set; } = null!;
  public static CoreVM VM { get; private set; } = null!;
  public Settings Settings { get; }
  public IImportPlugin? ImportPlugin { get; set; }
  public IImportPlugin[] ImportPlugins { get; private set; } = [];

  IPluginCoreVM IPluginCore.VM => VM;
  UserSettings IPluginCore.Settings => Settings;

  public Core() {
    Inst = this;
    BaseDir = Path.Combine("plugins", Name);
    PluginsDir = Path.Combine(BaseDir, "plugins");
    Settings = Settings.Load(Path.Combine(BaseDir, "settings.json"));
  }

  public Task InitAsync(PM.Core pmCore, PM.CoreR pmCoreR, IProgress<string> progress) {
    PMCore = pmCore;
    R = new(pmCoreR, this);

    return Task.Run(() => {
      R.AddDataAdapters();
      progress.Report("Migrating MovieManager Database");
      R.Migrate(0, DatabaseMigration.Resolver);
      R.LoadAllTables(progress);
      R.LinkReferences(progress);
      LoadPlugins(progress);
      R.ClearDataAdapters();
      R.SetIsReady();
    });
  }

  public void AfterInit(PM.CoreS pmCoreS, PM.CoreVM pmCoreVM) {
    S = new(pmCoreS, R);
    VM = new(pmCoreVM, S, R);
    R.SetFolders();
    R.AttachEvents();
    VM.AttachEvents();
  }

  private void LoadPlugins(IProgress<string> progress) {
    progress.Report("Loading Movie Manager plugins ...");

    try {
      ImportPlugins = Directory
        .EnumerateFiles(PluginsDir, "*.dll", SearchOption.TopDirectoryOnly)
        .Select(Plugin.LoadPlugin<IImportPlugin>)
        .Where(x => x != null)
        .Select(x => x!)
        .ToArray();

      ImportPlugin = ImportPlugins.FirstOrDefault();
      Plugins.Common.Core.IMDbPlugin = ImportPlugins.OfType<IIMDbPlugin>().SingleOrDefault();
    }
    catch (Exception ex) {
      Log.Error(ex);
    }
  }
}