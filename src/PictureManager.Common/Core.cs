using MH.Utils;
using MH.Utils.Extensions;
using PictureManager.Common.Interfaces.Plugin;
using PictureManager.Common.Repositories;
using PictureManager.Common.Services;
using PictureManager.Common.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Common;

public sealed class Core {
  private static Core _inst;
  private static readonly object _lock = new();
  public static Core Inst { get { lock (_lock) { return _inst ??= new(); } } }

  public static CoreR R { get; } = new();
  public static CoreS S { get; private set; }
  public static CoreVM VM { get; private set; }
  public static Settings Settings { get; } = Settings.Load();
  public List<IPluginCore> Plugins { get; } = [];

  private Core() {
    Tasks.SetUiTaskScheduler();
  }

  public Task InitAsync(IProgress<string> progress) {
    return Task.Run(async () => {
      R.AddDataAdapters();
      Drives.UpdateSerialNumbers();
      progress.Report("Migrating Database");
      R.Migrate(8, DatabaseMigration.Resolver);
      R.LoadAllTables(progress);
      R.LinkReferences(progress);
      await LoadPlugins(progress);
      R.ClearDataAdapters();
      R.SetIsReady();
      progress.Report("Loading UI");
    });
  }

  public void AfterInit() {
    S = new(R);
    VM = new(S, R);

    R.AttachEvents();
    S.AttachEvents();
    VM.AttachEvents();

    R.Folder.Tree.AddDrives();

    foreach (var plugin in Plugins) plugin.AfterInit(S, VM);

    AttachEvents();

    R.Keyword.Tree.AutoAddedGroup ??=
      R.CategoryGroup.ItemCreate(R.Keyword.Tree, "Auto Added");

    S.Viewer.SetCurrent(R.Viewer.All.SingleOrDefault(x => x.IsDefault));
    S.Viewer.Current?.UpdateHashSets();
    VM.MainWindow.TreeViewCategories.AddCategories();
    R.CategoryGroup.AddCategory(R.Person.Tree);
    R.CategoryGroup.AddCategory(R.Keyword.Tree);
    VM.Video.MediaPlayer.SetView(CoreVM.UiFullVideo);
    VM.Video.MediaPlayer.SetView(CoreVM.UiDetailVideo);
  }

  private async Task LoadPlugins(IProgress<string> progress) {
    if (!Directory.Exists("plugins")) return;
    progress.Report("Loading Picture Manager plugins ...");

    try {
      foreach (var pluginDir in Directory.EnumerateDirectories("plugins", "*", SearchOption.TopDirectoryOnly)) {
        var pluginPath = Path.Combine(pluginDir, $"{pluginDir.Split(Path.DirectorySeparatorChar)[1]}.Common.dll");
        if (!File.Exists(pluginPath) || Plugin.LoadPlugin<IPluginCore>(pluginPath) is not { } plugin) continue;
        await plugin.InitAsync(this, R, progress);
        Plugins.Add(plugin);
      }
    }
    catch (Exception ex) {
      Log.Error(ex);
    }
  }

  private void AttachEvents() {
    Settings.GeoName.PropertyChanged += (_, e) => {
      if (e.Is(nameof(Settings.GeoName.UserName)))
        R.GeoName.ApiLimitExceeded = false;
    };
  }
}