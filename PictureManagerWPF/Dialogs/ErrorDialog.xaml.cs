using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PictureManager.Dialogs {
  /// <summary>
  /// Interaction logic for ErrorDialog.xaml
  /// </summary>
  public partial class ErrorDialog : INotifyPropertyChanged {
    private string _titleText;
    private string _message;
    private string _detail;

    public string TitleText { get => _titleText; set { _titleText = value; OnPropertyChanged(); } }
    public string Message { get => _message; set { _message = value; OnPropertyChanged(); } }
    public string Detail { get => _detail; set { _detail = value; OnPropertyChanged(); } }

    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public ErrorDialog() {
      InitializeComponent();
    }

    public static void Show(Exception ex) {
      Show(ex, string.Empty);
    }

    public static void Show(Exception ex, string title) {
      var ed = new ErrorDialog {
        Owner = App.WMain,
        TitleText = string.IsNullOrEmpty(title) ? "Error" : title,
        Message = ex.Message,
        Detail = ex.InnerException == null ? ex.StackTrace : $"{ex.InnerException.Message}\n{ex.StackTrace}"
      };
      ed.ShowDialog();
    }
  }
}
