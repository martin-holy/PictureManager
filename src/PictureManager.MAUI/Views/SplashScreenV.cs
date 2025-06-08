using Microsoft.Maui.Controls;
using System;
using System.Diagnostics;
using System.Reflection;

namespace PictureManager.MAUI.Views;

public partial class SplashScreenV : ContentPage {
  private string? _message;

  public string Version { get; }
  public string? Message { get => _message; set { _message = value; OnPropertyChanged(); } }
  public IProgress<string> ProgressMessage { get; set; }

  public SplashScreenV() {
    InitializeComponent();
    BindingContext = this;
    ProgressMessage = new Progress<string>(msg => Message = msg);
    Version = $"ver.: {FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion}";
  }
}