using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Domain.Utils;
using PictureManager.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PictureManager.Dialogs {
  public partial class ResizeImagesDialog : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private CancellationTokenSource _cts;
    private readonly MediaItemM[] _items;
    private bool _error;
    private string _fileName;
    private double _maxMpx;
    private ObservableCollection<string> _dirPaths;

    public bool Error { get => _error; set { _error = value; OnPropertyChanged(); } }
    public string FileName { get => _fileName; set { _fileName = value; OnPropertyChanged(); } }
    public double MaxMpx { get => _maxMpx; set { _maxMpx = value; OnPropertyChanged(); } }
    public ObservableCollection<string> DirPaths { get => _dirPaths; set { _dirPaths = value; OnPropertyChanged(); } }

    public ResizeImagesDialog(IEnumerable<MediaItemM> items) {
      InitializeComponent();
      Owner = Application.Current.MainWindow;
      _items = items.Where(x => x.MediaType == MediaType.Image).ToArray();

      DirPaths = new(Settings.Default.DirectorySelectFolders.Split(new[] { Environment.NewLine },
        StringSplitOptions.RemoveEmptyEntries));

      SetMaxMpx();
    }

    public static void Show(IEnumerable<MediaItemM> items) {
      var dlg = new ResizeImagesDialog(items);
      dlg.Show();
    }

    private void SetMaxMpx() {
      var maxPx = 0;
      foreach (var mi in _items) {
        var px = mi.Width * mi.Height;
        if (px > maxPx) maxPx = px;
      }

      MaxMpx = Math.Round(maxPx / 1000000.0, 1);
      SldMpx.Value = MaxMpx;
    }

    private async void ResizeImages(string destination, int px, bool withMetadata, bool withThumbnail) {
      _cts = new();

      PbProgress.Value = 0;
      PbProgress.Maximum = _items.Length;

      await Task.Run(() => {
        try {
          var index = 0;
          var po = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = _cts.Token };

          Parallel.ForEach(_items, po, mi => {
            index++;
            Core.RunOnUiThread(() => {
              PbProgress.Value = index;
              FileName = mi.FileName;
            });

            try {
              var dest = Path.Combine(destination, mi.FileName);
              Imaging.ResizeJpg(mi.FilePath, dest, px, withMetadata, withThumbnail, Settings.Default.JpegQualityLevel);
            }
            catch (Exception ex) {
              App.Core.LogError(ex, mi.FilePath);
            }
          });
        }
        catch (OperationCanceledException) { }
        finally {
          _cts.Dispose();
          _cts = null;
        }
      });

      Close();
    }

    private void BtnResize_OnClick(object sender, RoutedEventArgs e) {
      if (_cts != null) {
        _cts.Cancel();
        return;
      }

      var destination = CmbDirPaths.Text;

      if (!Directory.Exists(destination)) {
        try {
          Directory.CreateDirectory(destination);
        }
        catch (Exception ex) {
          App.Core.LogError(ex);
          Error = true;
          return;
        }
      }

      BtnResize.Content = "Cancel";
      ResizeImages(destination,
        Convert.ToInt32(SldMpx.Value * 1000000),
        ChbWithMetadata.IsChecked == true,
        ChbWithThumbnail.IsChecked == true);
    }

    private void BtnOpenDirectoryPicker_OnClick(object sender, RoutedEventArgs e) {
      var dir = new FolderBrowserDialog();
      if (!(dir.ShowDialog() ?? true)) return;

      if (!DirPaths.Contains(dir.SelectedPath)) {
        DirPaths.Insert(0, dir.SelectedPath);
        Settings.Default.DirectorySelectFolders = string.Join(Environment.NewLine, DirPaths);
        Settings.Default.Save();
      }

      CmbDirPaths.SelectedIndex = 0;
    }

    private void CmbDirPaths_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => Error = false;
  }
}
