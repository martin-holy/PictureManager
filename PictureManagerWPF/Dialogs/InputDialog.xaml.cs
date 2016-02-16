using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace PictureManager.Dialogs {
  /// <summary>
  /// Interaction logic for InputDialog.xaml
  /// </summary>
  public partial class InputDialog: INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged(string name) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private string _iconName = "appbar_bug";
    private string _question;
    private string _answer;
    private bool _error;

    public string IconName { get { return _iconName;} set { _iconName = value; OnPropertyChanged("IconName"); } }
    public string Question { get { return _question; } set { _question = value; OnPropertyChanged("Question"); } }
    public string Answer { get { return _answer; } set { _answer = value; OnPropertyChanged("Answer"); } }
    public bool Error { get { return _error; } set { _error = value; OnPropertyChanged("Error"); } }

    public InputDialog() {
      InitializeComponent();
      TxtAnswer.Focus();
    }

    private void BtnDialogOk_OnClick(object sender, RoutedEventArgs e) {
      DialogResult = true;
    }

    public void ShowErrorMessage(string text) {
      TxtAnswer.ToolTip = text;
      Error = true;
    }
  }
}
