using Avalonia.Controls;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace PictureManager.AvaloniaUI.Views;

public partial class SplashScreenV : Window, INotifyPropertyChanged {
  public new event PropertyChangedEventHandler? PropertyChanged = delegate { };
  public void OnPropertyChanged([CallerMemberName] string? name = null) =>
    PropertyChanged!(this, new(name));

  private string? _message;

  public string Version { get; set; }
  public string? Message { get => _message; set { _message = value; OnPropertyChanged(); } }
  public IProgress<string> ProgressMessage { get; set; }

  public SplashScreenV() {
    InitializeComponent();

    ProgressMessage = new Progress<string>(msg => Message = msg);

    Version = $"ver.: {FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion}";
  }
}