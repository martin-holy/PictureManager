using MH.Utils;
using MovieManager.Common.Repositories;
using MovieManager.Common.Services;
using MovieManager.Common.ViewModels;
using System;
using System.Threading.Tasks;

namespace MovieManager.Common;

public sealed class Core {
  private static Core _inst;
  private static readonly object _lock = new();
  public static Core Inst { get { lock (_lock) { return _inst ??= new(); } } }

  public static CoreR R { get; } = new();
  public static CoreS S { get; private set; }
  public static CoreVM VM { get; private set; }

  public Task InitAsync(IProgress<string> progress) {
    return Task.Run(() => {
      R.AddDataAdapters();
      Drives.UpdateSerialNumbers();
      progress.Report("Migrating MovieManager Database");
      SimpleDB.Migrate(0, DatabaseMigration.Resolver);
      R.LoadAllTables(progress);
      R.LinkReferences(progress);
      R.ClearDataAdapters();
      R.SetIsReady();
      progress.Report("Loading UI");
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