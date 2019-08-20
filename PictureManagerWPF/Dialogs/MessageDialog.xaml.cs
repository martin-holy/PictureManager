using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PictureManager.Dialogs {
  /// <summary>
  /// Interaction logic for InputDialog.xaml
  /// </summary>
  public partial class MessageDialog : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private IconName _iconName = IconName.Bug;
    private string _titleText;
    private string _message;
    private bool _canCancel;

    public IconName IconName { get => _iconName; set { _iconName = value; OnPropertyChanged(); } }
    public string TitleText { get => _titleText; set { _titleText = value; OnPropertyChanged(); } }
    public string Message { get => _message; set { _message = value; OnPropertyChanged(); } }
    public bool CanCancel { get => _canCancel; set { _canCancel = value; OnPropertyChanged(); } }

    public MessageDialog() {
      InitializeComponent();
    }

    public static bool Show(string title, string message, bool canCancel) {
      var result = false;

      var md = new MessageDialog {
        TitleText = title,
        Message = message,
        IconName = canCancel ? IconName.Question : IconName.Information,
        CanCancel = canCancel
      };

      md.BtnOk.Click += delegate {
        md.Close();
        result = true;
      };

      md.BtnCancel.Click += delegate {
        md.Close();
        result = false;
      };

      md.ShowDialog();

      return result;
    }
  }
}
