using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace PictureManager.Dialogs {
  public partial class MessageDialog : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged = delegate { };
    public void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged.Invoke(this, new(name));

    private string _titleText;
    private string _message;
    private bool _canCancel;

    public string TitleText { get => _titleText; set { _titleText = value; OnPropertyChanged(); } }
    public string Message { get => _message; set { _message = value; OnPropertyChanged(); } }
    public bool CanCancel { get => _canCancel; set { _canCancel = value; OnPropertyChanged(); } }

    public MessageDialog(string title, string message, bool canCancel, string[] buttons = null) {
      InitializeComponent();

      TitleText = title;
      Message = message;
      CanCancel = canCancel;
      Owner = Application.Current.MainWindow;
      if (buttons == null)
        BtnOk.Content = canCancel ? "YES" : "OK";
      else {
        BtnOk.Content = buttons[0];
        BtnNo.Content = buttons[1];
      }
    }

    public static bool Show(string title, string message, bool canCancel, string[] buttons = null) {
      var result = false;
      var md = new MessageDialog(title, message, canCancel, buttons);

      md.BtnOk.Click += delegate {
        md.Close();
        result = true;
      };

      md.BtnNo.Click += delegate {
        md.Close();
        result = false;
      };

      _ = md.ShowDialog();

      return result;
    }

    public static bool? Show(string title, string message, bool canCancel, string buttonYes, string buttonNo) {
      bool? result = null;
      var md = new MessageDialog(title, message, canCancel, new [] { buttonYes, buttonNo });

      md.BtnOk.Click += (_, _) => {
        result = true;
        md.Close();
      };

      md.BtnNo.Click += (_, _) => {
        result = false;
        md.Close();
      };

      _ = md.ShowDialog();

      return result;
    }
  }
}
