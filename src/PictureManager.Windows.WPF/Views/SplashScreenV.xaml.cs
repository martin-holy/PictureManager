using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace PictureManager.Windows.WPF.Views;

public partial class SplashScreenV : INotifyPropertyChanged {
  public event PropertyChangedEventHandler? PropertyChanged = delegate { };
  public void OnPropertyChanged([CallerMemberName] string? name = null) =>
    PropertyChanged!(this, new(name));

  private string? _message;

  public string Version { get; }
  public string? Message { get => _message; set { _message = value; OnPropertyChanged(); } }
  public IProgress<string> ProgressMessage { get; set; }

  public SplashScreenV() {
    InitializeComponent();

    ProgressMessage = new Progress<string>(msg => Message = msg);

    Version = $"ver.: {FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion}";
  }
}