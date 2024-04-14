using MH.Utils;
using MH.Utils.Extensions;
using PictureManager.Common.Interfaces;
using PictureManager.Common.Repositories;
using PictureManager.Common.Services;
using PictureManager.Common.ViewModels;
using System;
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

  public static Func<double> GetDisplayScale { get; set; }

  private Core() {
    Tasks.SetUiTaskScheduler();
  }

  public Task InitAsync(IProgress<string> progress) {
    return Task.Run(() => {
      R.AddDataAdapters();
      Drives.UpdateSerialNumbers();
      progress.Report("Migrating Database");
      SimpleDB.Migrate(8, DatabaseMigration.Resolver);
      R.LoadAllTables(progress);
      R.LinkReferences(progress);
      R.ClearDataAdapters();
      R.SetIsReady();
      progress.Report("Loading UI");
    });
  }

  public void AfterInit() {
    var scale = GetDisplayScale();
    SegmentS.SetSegmentUiSize(scale);
    S = new(R);
    VM = new(S, R);

    R.AttachEvents();
    S.AttachEvents();
    VM.AttachEvents();

    AttachEvents();

    R.Keyword.Tree.AutoAddedGroup ??=
      R.CategoryGroup.ItemCreate(R.Keyword.Tree, "Auto Added");

    R.Folder.Tree.AddDrives();
    S.Viewer.SetCurrent(R.Viewer.All.SingleOrDefault(x => x.IsDefault));
    S.Viewer.Current?.UpdateHashSets();
    VM.MainWindow.TreeViewCategories.AddCategories();
    R.CategoryGroup.AddCategory(R.Person.Tree);
    R.CategoryGroup.AddCategory(R.Keyword.Tree);
    VM.Video.MediaPlayer.SetView(CoreVM.UiFullVideo);
    VM.Video.MediaPlayer.SetView(CoreVM.UiDetailVideo);
  }

  private void AttachEvents() {
    Settings.GeoName.PropertyChanged += (_, e) => {
      if (e.Is(nameof(Settings.GeoName.UserName)))
        R.GeoName.ApiLimitExceeded = false;
    };
  }
}