using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PictureManager.Dialogs {
  /// <summary>
  /// Interaction logic for InputDialog.xaml
  /// </summary>
  public partial class InputDialog: INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = "") {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private string _iconName = "appbar_bug";
    private string _question;
    private string _answer;
    private bool _error;

    public string IconName { get { return _iconName;} set { _iconName = value; OnPropertyChanged(); } }
    public string Question { get { return _question; } set { _question = value; OnPropertyChanged(); } }
    public string Answer { get { return _answer; } set { _answer = value; OnPropertyChanged(); } }
    public bool Error { get { return _error; } set { _error = value; OnPropertyChanged(); } }

    public InputDialog() {
      InitializeComponent();
      TxtAnswer.Focus();
    }

    public void ShowErrorMessage(string text) {
      TxtAnswer.ToolTip = text;
      Error = true;
    }
  }
}
