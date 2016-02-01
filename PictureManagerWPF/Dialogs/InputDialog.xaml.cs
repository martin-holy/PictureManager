using System.ComponentModel;
using System.Windows;

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

    public string IconName { get { return _iconName;} set { _iconName = value; OnPropertyChanged("IconName"); } }
    public string Question { get { return _question; } set { _question = value; OnPropertyChanged("Question"); } }
    public string Answer { get { return _answer; } set { _answer = value; OnPropertyChanged("Answer"); } }

    public InputDialog() {
      InitializeComponent();
    }

    private void BtnDialogOk_OnClicktnDialogOk_Click(object sender, RoutedEventArgs e) {
      DialogResult = true;
    }
  }
}
