using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using PictureManager.Database;
using PictureManager.Properties;

namespace PictureManager.Dialogs {
  public partial class ImagesToVideoDialog : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private readonly MediaItem[] _items;
    private int _quality;
    private double _speed;
    private string _scale;
    private ObservableCollection<string> _scales;
    private Process _process;
    private readonly string _inputListPath;
    private readonly string _outputFilePath;

    public int Quality { get => _quality; set { _quality = value; OnPropertyChanged(); } }
    public double Speed { get => _speed; set { _speed = value; OnPropertyChanged(); } }
    public string Scale { get => _scale; set { _scale = value; OnPropertyChanged(); } }
    public ObservableCollection<string> Scales { get => _scales; set { _scales = value; OnPropertyChanged(); } }

    public ImagesToVideoDialog(Window owner, IEnumerable<MediaItem> items) {
      InitializeComponent();
      Owner = owner;
      _items = items.Where(x => x.MediaType == MediaType.Image).ToArray();
      _inputListPath = Path.GetTempPath() + "PictureManagerImagesToVideo.list";
      _outputFilePath = _items.First().FilePath + ".mp4";

      Scales = new ObservableCollection<string>(
        Settings.Default.ImagesToVideoScales.Split(new[] { Environment.NewLine },
          StringSplitOptions.RemoveEmptyEntries));

      // Preset Default Values
      if (Scales.Count == 0) {
        Scales.Add("1440x1080");
        Scales.Add("1920x1080");
      }

      Scale = Scales[0];
      Speed = 0.25;
      Quality = 27;
    }

    public static void Show(Window owner, IEnumerable<MediaItem> items) {
      var dlg = new ImagesToVideoDialog(owner, items);
      dlg.Show();
    }

    // create input list of items for FFMPEG
    private bool CreateTempListFile() {
      try {
        using (var sw = new StreamWriter(_inputListPath, false, new UTF8Encoding(false), 65536)) {
          foreach (var item in _items)
            sw.WriteLine($"file '{item.FilePath}'");
        }

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
      var speedStr = Speed.ToString(CultureInfo.InvariantCulture);
      var args = $"-y -r 1/{speedStr} -f concat -safe 0 -i \"{_inputListPath}\" -c:v libx264 -r 25 -preset medium -crf {Quality} -vf \"scale={Scale},format=yuv420p\" \"{_outputFilePath}\"";
      var tcs = new TaskCompletionSource<bool>();

      _process = new Process {
        EnableRaisingEvents = true,
        StartInfo = new ProcessStartInfo {
          Arguments = args,
          FileName = Settings.Default.FfmpegPath,
          UseShellExecute = false,
          CreateNoWindow = false
        }
      };

      _process.Exited += (s, e) => {
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

      if (!Scales.Contains(Scale)) {
        Scales.Insert(0, Scale);
        Settings.Default.ImagesToVideoScales = string.Join(Environment.NewLine, Scales);
        Settings.Default.Save();
      }

      // check for FFMPEG
      if (!File.Exists(Settings.Default.FfmpegPath)) {
        MessageDialog.Show(
          "FFMPEG not found", 
          "FFMPEG was not found. Install it and set the path in the settings.",
          false);
        Close();
        return;
      }

      PbProgress.IsIndeterminate = true;
      BtnCreateVideo.Content = "Cancel";

      if (CreateTempListFile()) {
        await CreateVideoAsync();
        DeleteFile(_inputListPath);
        Owner.Activate();
        // TODO soft reload. keep selected and scroll, just add one thumbnail
      }

      Close();
    }
  }
}
