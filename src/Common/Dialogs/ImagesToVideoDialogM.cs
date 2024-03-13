﻿using MH.UI.Controls;
using MH.UI.Dialogs;
using MH.Utils;
using MH.Utils.Extensions;
using PictureManager.Common.Models;
using PictureManager.Common.Models.MediaItems;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PictureManager.Common.Dialogs {
  public sealed class ImagesToVideoDialogM : Dialog {
    private bool _isBusy;
    private readonly ImageM[] _items;
    private Process _process;
    private readonly string _inputListPath;
    private readonly string _outputFilePath;
    private readonly string _outputFileName;
    private readonly FolderM _outputFolder;
    private readonly OnSuccess _onSuccess;

    public delegate void OnSuccess(FolderM folder, string fileName);
    public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }

    public ImagesToVideoDialogM(ImageM[] items, OnSuccess onSuccess) : base("Images to Video", MH.UI.Res.IconMovieClapper) {
      Buttons = new DialogButton[] {
        new(new(CreateVideo, () => !IsBusy, MH.UI.Res.IconMovieClapper, "Create Video"), true),
        new(CloseCommand, false, true) };

      _items = items;
      var firstMi = _items.First();
      var fileName = IOExtensions.GetNewFileName(firstMi.Folder.FullPath, firstMi.FileName + ".mp4");

      _inputListPath = Path.GetTempPath() + "PictureManagerImagesToVideo.list";
      _outputFilePath = IOExtensions.PathCombine(firstMi.Folder.FullPath, fileName);
      _outputFileName = fileName;
      _outputFolder = firstMi.Folder;
      _onSuccess = onSuccess;
    }

    public override Task OnResultChanged(int result) {
      if (result == 0 && _process != null) {
        _process.Kill();
        DeleteFile(_inputListPath);
        DeleteFile(_outputFilePath);
      }

      return Task.CompletedTask;
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
        Log.Error(ex);
        return false;
      }
    }

    private static void DeleteFile(string path) {
      try {
        if (File.Exists(path))
          File.Delete(path);
      }
      catch (Exception ex) {
        Log.Error(ex);
      }
    }

    public Task CreateVideoAsync() {
      var mi = _items.First();

      // Scale
      var height = (double)Core.Settings.ImagesToVideo.Height;
      var width = mi.Orientation is Orientation.Rotate270 or Orientation.Rotate90
        ? Math.Round(mi.Height / (mi.Width / height), 0)
        : Math.Round(mi.Width / (mi.Height / height), 0);
      if (width % 2 != 0) width++;
      var scale = $"{width}x{height}";

      // Rotate
      var rotation = mi.Orientation switch {
        Orientation.Rotate180 => "transpose=clock,transpose=clock,",
        Orientation.Rotate270 => "transpose=clock:passthrough=portrait,",
        Orientation.Rotate90 => "transpose=cclock:passthrough=portrait,",
        _ => string.Empty
      };

      var speedStr = Core.Settings.ImagesToVideo.Speed.ToString(CultureInfo.InvariantCulture);
      var args = $"-y -r 1/{speedStr} -f concat -safe 0 -i \"{_inputListPath}\" -c:v libx264 -r 25 -preset medium -crf {Core.Settings.ImagesToVideo.Quality} -vf \"{rotation}scale={scale},format=yuv420p\" \"{_outputFilePath}\"";
      var tcs = new TaskCompletionSource<bool>();

      _process = new() {
        EnableRaisingEvents = true,
        StartInfo = new() {
          Arguments = args,
          FileName = Core.Settings.Common.FfmpegPath,
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

    private async void CreateVideo() {
      Core.Settings.Save();

      // check for FFMPEG
      if (!File.Exists(Core.Settings.Common.FfmpegPath)) {
        Show(new MessageDialog(
          "FFMPEG not found",
          "FFMPEG was not found. Install it and set the path in the settings.",
          Res.IconInformation,
          false));
        Result = 0;
        return;
      }

      IsBusy = true;

      if (CreateTempListFile()) {
        await CreateVideoAsync();
        DeleteFile(_inputListPath);
        _onSuccess.Invoke(_outputFolder, _outputFileName);
      }

      Result = 1;
    }
  }
}
