using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Common.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PictureManager.Common.Services;

namespace PictureManager.Common.Dialogs;

public sealed class ExportSegmentsDialog : Dialog {
  private CancellationTokenSource _cts;
  private readonly SegmentM[] _items;
  private string _fileName;
  private string _destDir;
  private int _progressMax;
  private int _progressValue;
  private ObservableCollection<string> _dirPaths;

  public string FileName { get => _fileName; set { _fileName = value; OnPropertyChanged(); } }
  public string DestDir { get => _destDir; set { _destDir = value; OnPropertyChanged(); } }
  public int ProgressMax { get => _progressMax; set { _progressMax = value; OnPropertyChanged(); } }
  public int ProgressValue { get => _progressValue; set { _progressValue = value; OnPropertyChanged(); } }
  public ObservableCollection<string> DirPaths { get => _dirPaths; set { _dirPaths = value; OnPropertyChanged(); } }

  public RelayCommand OpenFolderBrowserCommand { get; }

  public ExportSegmentsDialog(SegmentM[] items) : base("Export Segments", Res.IconSegment) {
    OpenFolderBrowserCommand = new(OpenFolderBrowser, Res.IconFolder, "Select folder");
    Buttons = [
      new(new(Export, null, "Export"), true),
      new(CloseCommand, false, true)
    ]; 
    _items = items;
    ProgressMax = _items.Length;
    DirPaths = new(Core.Settings.Common.DirectorySelectFolders.EmptyIfNull());
  }

  public override Task OnResultChanged(int result) {
    if (result == 0 && _cts != null) _cts.Cancel();
    return Task.CompletedTask;
  }

  private void Export() {
    if (!Directory.Exists(DestDir)) {
      try {
        Directory.CreateDirectory(DestDir);
      }
      catch (Exception ex) {
        Log.Error(ex);
        return;
      }
    }

    ExportSegments(DestDir);
  }

  private async void ExportSegments(string destination) {
    _cts = new();

    await Task.Run(() => {
      try {
        var index = 0;
        var po = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = _cts.Token };

        Parallel.ForEach(_items, po, s => {
          index++;
          var fileName = s.MediaItem.FileName.Replace(".jpg", "_segment_" + s.Id + ".jpg", StringComparison.OrdinalIgnoreCase);

          Tasks.RunOnUiThread(() => {
            ProgressValue = index;
            FileName = fileName;
          });

          try {
            var dest = Path.Combine(destination, fileName);
            SegmentS.ExportSegment(s, dest);
          }
          catch (Exception ex) {
            Log.Error(ex, s.MediaItem.FilePath);
          }
        });
      }
      catch (OperationCanceledException) { }
      finally {
        _cts.Dispose();
        _cts = null;
        Tasks.RunOnUiThread(() => { Result = 1; });
      }
    });
  }

  private void OpenFolderBrowser() {
    var dir = new FolderBrowserDialogM();

    if (Show(dir) != 1) return;

    if (!DirPaths.Contains(dir.SelectedFolder.FullPath)) {
      DirPaths.Insert(0, dir.SelectedFolder.FullPath);
      Core.Settings.Common.DirectorySelectFolders = DirPaths.ToArray();
      Core.Settings.Save();
    }

    DestDir = dir.SelectedFolder.FullPath;
  }
}