using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PictureManager.Dialogs {
  public partial class FileOperationDialog : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private string _dirFrom;
    private string _dirTo;
    private string _fileName;

    public CancellationTokenSource LoadCts { get; set; }
    public Task RunTask { get; set; }
    public IProgress<object[]> Progress;
    public string DirFrom { get => _dirFrom; set { _dirFrom = value; OnPropertyChanged(); } }
    public string DirTo { get => _dirTo; set { _dirTo = value; OnPropertyChanged(); } }
    public string FileName { get => _fileName; set { _fileName = value; OnPropertyChanged(); } }

    public FileOperationDialog() {
      InitializeComponent();

      Progress = new Progress<object[]>(e => {
        PbProgress.Value = (int) e[0];
        DirFrom = e[1].ToString();
        DirTo = e[2].ToString();
        FileName = e[3].ToString();
      });
    }

    ~FileOperationDialog() {
      if (LoadCts != null) {
        LoadCts.Dispose();
        LoadCts = null;
      }
    }

    private async void BtnCancel_OnClick(object sender, RoutedEventArgs e) {
      if (LoadCts != null) {
        LoadCts.Cancel();
        await RunTask;
        Close();
      }
    }
  }
}
