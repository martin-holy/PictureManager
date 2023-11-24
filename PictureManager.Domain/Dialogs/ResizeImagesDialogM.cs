using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Domain.Dialogs {
  public sealed class ResizeImagesDialogM : Dialog {
    private CancellationTokenSource _cts;
    private readonly MediaItemM[] _items;
    private bool _preserveThumbnail;
    private bool _preserveMetadata;
    private string _fileName;
    private string _destDir;
    private double _mpx;
    private double _maxMpx;
    private int _progressMax;
    private int _progressValue;
    private ObservableCollection<string> _dirPaths;

    public bool PreserveThumbnail { get => _preserveThumbnail; set { _preserveThumbnail = value; OnPropertyChanged(); } }
    public bool PreserveMetadata { get => _preserveMetadata; set { _preserveMetadata = value; OnPropertyChanged(); } }
    public string FileName { get => _fileName; set { _fileName = value; OnPropertyChanged(); } }
    public string DestDir { get => _destDir; set { _destDir = value; OnPropertyChanged(); } }
    public double Mpx { get => _mpx; set { _mpx = value; OnPropertyChanged(); } }
    public double MaxMpx { get => _maxMpx; set { _maxMpx = value; OnPropertyChanged(); } }
    public int ProgressMax { get => _progressMax; set { _progressMax = value; OnPropertyChanged(); } }
    public int ProgressValue { get => _progressValue; set { _progressValue = value; OnPropertyChanged(); } }
    public ObservableCollection<string> DirPaths { get => _dirPaths; set { _dirPaths = value; OnPropertyChanged(); } }

    public RelayCommand<object> OpenFolderBrowserCommand { get; }

    public ResizeImagesDialogM(IEnumerable<MediaItemM> items) : base("Resize Images", Res.IconImageMultiple) {
      OpenFolderBrowserCommand = new(OpenFolderBrowser);
      CloseCommand = new(Cancel);

      Buttons = new DialogButton[] {
        new("Resize", null, new(Resize), true),
        new("Cancel", Res.IconXCross, CloseCommand, false, true) }; 

      _items = items.Where(x => x.MediaType == MediaType.Image).ToArray();
      ProgressMax = _items.Length;

      DirPaths = new(Core.Settings.DirectorySelectFolders.Split(','));

      SetMaxMpx();
    }

    private void SetMaxMpx() {
      var maxPx = 0;
      foreach (var mi in _items) {
        var px = mi.Width * mi.Height;
        if (px > maxPx) maxPx = px;
      }

      MaxMpx = Math.Round(maxPx / 1000000.0, 1);
      Mpx = MaxMpx;
    }

    private void Resize() {
      if (!Directory.Exists(DestDir)) {
        try {
          Directory.CreateDirectory(DestDir);
        }
        catch (Exception ex) {
          Log.Error(ex);
          return;
        }
      }

      ResizeImages(DestDir, Convert.ToInt32(Mpx * 1000000), PreserveMetadata, PreserveThumbnail);
    }

    private async void ResizeImages(string destination, int px, bool withMetadata, bool withThumbnail) {
      _cts = new();

      await Task.Run(() => {
        try {
          var index = 0;
          var po = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = _cts.Token };

          Parallel.ForEach(_items, po, mi => {
            index++;
            Tasks.RunOnUiThread(() => {
              ProgressValue = index;
              FileName = mi.FileName;
            });

            try {
              var dest = Path.Combine(destination, mi.FileName);
              Imaging.ResizeJpg(mi.FilePath, dest, px, withMetadata, withThumbnail, Core.Settings.JpegQualityLevel);
            }
            catch (Exception ex) {
              Log.Error(ex, mi.FilePath);
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

    private void Cancel() {
      if (_cts != null)
        _cts.Cancel();

      Result = 0;
    }

    private void OpenFolderBrowser() {
      var dir = new FolderBrowserDialogM();

      if (Show(dir) != 1) return;

      if (!DirPaths.Contains(dir.SelectedFolder.FullPath)) {
        DirPaths.Insert(0, dir.SelectedFolder.FullPath);
        Core.Settings.DirectorySelectFolders = string.Join(',', DirPaths);
        Core.Settings.Save();
      }

      DestDir = dir.SelectedFolder.FullPath;
    }
  }
}
