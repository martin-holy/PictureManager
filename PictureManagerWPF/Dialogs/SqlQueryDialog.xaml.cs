using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace PictureManager.Dialogs {
  /// <summary>
  /// Interaction logic for InputDialog.xaml
  /// </summary>
  public partial class SqlQueryDialog: INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = "") {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private string _iconName = "appbar_bug";
    private string _sqlQueryName;
    private string _sqlQueryQuery;
    private bool _error;

    public string IconName { get => _iconName; set { _iconName = value; OnPropertyChanged(); } }
    public string SqlQueryName { get => _sqlQueryName; set { _sqlQueryName = value; OnPropertyChanged(); } }
    public string SqlQueryQuery { get => _sqlQueryQuery; set { _sqlQueryQuery = value; OnPropertyChanged(); } }
    public bool Error { get => _error; set { _error = value; OnPropertyChanged(); } }

    public SqlQueryDialog() {
      InitializeComponent();
      TxtName.Focus();
    }

    public void ShowErrorMessage(string text) {
      TxtName.ToolTip = text;
      Error = true;
    }

    private void TxtName_OnKeyUp(object sender, KeyEventArgs e) {
      SqlQueryName = TxtName.Text;
    }
  }
}
