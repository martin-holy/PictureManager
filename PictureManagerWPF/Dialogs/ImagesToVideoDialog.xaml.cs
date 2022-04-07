using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MH.Utils.Dialogs;
using MH.Utils.Extensions;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Properties;

namespace PictureManager.Dialogs {
  public partial class ImagesToVideoDialog {
    private readonly MediaItemM[] _items;
    private Process _process;
    private readonly string _inputListPath;
    private readonly string _outputFilePath;
    private readonly string _outputFileName;
    private readonly FolderM _outputFolder;
    private readonly OnSuccess _onSuccess;

    public delegate void OnSuccess(FolderM folder, string fileName);

    public ImagesToVideoDialog(IEnumerable<MediaItemM> items, OnSuccess onSuccess) {
      InitializeComponent();

      _items = items.ToArray();
      var firstMi = _items.First();
      var fileName = IOExtensions.GetNewFileName(firstMi.Folder.FullPath, firstMi.FileName + ".mp4");

      Owner = Application.Current.MainWindow;
      _inputListPath = Path.GetTempPath() + "PictureManagerImagesToVideo.list";
      _outputFilePath = IOExtensions.PathCombine(firstMi.Folder.FullPath, fileName);
      _outputFileName = fileName;
      _outputFolder = firstMi.Folder;
      _onSuccess = onSuccess;
    }

    public static void Show(IEnumerable<MediaItemM> items, OnSuccess onSuccess) {
      var dlg = new ImagesToVideoDialog(items, onSuccess);
      dlg.ShowDialog();
    }

    // create input list of items for FFMPEG
    private bool CreateTempListFile() {
      try {
        using var sw = new StreamWriter(_inputListPath, false, new UTF8Encoding(false), 65536);
        foreach (var item in _items)
          sw.WriteLine($"file '{item.FilePath}'");

        return true;
      }
      catch (Exception ex) {
        App.Core.LogError(ex);
        return false;
      }
    }

    private static void DeleteFile(string path) {
      try {
        if (File.Exists(path))
          File.Delete(path);
      }
      catch (Exception ex) {
        App.Core.LogError(ex);
      }
    }

    public Task CreateVideoAsync() {
      var mi = _items.First();

      // Scale
      var height = (double)Settings.Default.ImagesToVideoHeight;
      var width = mi.Orientation is (int)MediaOrientation.Rotate270 or (int)MediaOrientation.Rotate90
        ? Math.Round(mi.Height / (mi.Width / height), 0)
        : Math.Round(mi.Width / (mi.Height / height), 0);
      if (width % 2 != 0) width++;
      var scale = $"{width}x{height}";

      // Rotate
      var rotation = (MediaOrientation)mi.Orientation switch {
        MediaOrientation.Rotate180 => "transpose=clock,transpose=clock,",
        MediaOrientation.Rotate270 => "transpose=clock:passthrough=portrait,",
        MediaOrientation.Rotate90 => "transpose=cclock:passthrough=portrait,",
        _ => string.Empty
      };

      var speedStr = Settings.Default.ImagesToVideoSpeed.ToString(CultureInfo.InvariantCulture);
      var args = $"-y -r 1/{speedStr} -f concat -safe 0 -i \"{_inputListPath}\" -c:v libx264 -r 25 -preset medium -crf {Settings.Default.ImagesToVideoQuality} -vf \"{rotation}scale={scale},format=yuv420p\" \"{_outputFilePath}\"";
      var tcs = new TaskCompletionSource<bool>();

      _process = new() {
        EnableRaisingEvents = true,
        StartInfo = new() {
          Arguments = args,
          FileName = Settings.Default.FfmpegPath,
          UseShellExecute = false,
          CreateNoWindow = false
        }
      };

      _process.Exited += (_, _) => {
        tcs.TrySetResult(true);
        _process.Dispose();
      };

      _process.Start();
      return tcs.Task;
    }

    private async void BtnCreateVideo_OnClick(object sender, RoutedEventArgs e) {
      if (_process != null) {
        _process.Kill();
        DeleteFile(_inputListPath);
        DeleteFile(_outputFilePath);
        Close();
        return;
      }

      Settings.Default.Save();

      // check for FFMPEG
      if (!File.Exists(Settings.Default.FfmpegPath)) {
        Core.DialogHostShow(new MessageDialog(
          "FFMPEG not found",
          "FFMPEG was not found. Install it and set the path in the settings.",
          "IconInformation",
          false));
        Close();
        return;
      }

      PbProgress.IsIndeterminate = true;
      BtnCreateVideo.Content = "Cancel";

      if (CreateTempListFile()) {
        await CreateVideoAsync();
        DeleteFile(_inputListPath);
        Owner.Activate();
        _onSuccess.Invoke(_outputFolder, _outputFileName);
      }

      Close();
    }
  }
}
