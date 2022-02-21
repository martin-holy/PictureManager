using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace PictureManager.Dialogs {
  public partial class InputDialog : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private string _iconName = "IconBug";
    private string _question;
    private string _answer;
    private bool _error;

    public string IconName { get => _iconName; set { _iconName = value; OnPropertyChanged(); } }
    public string Question { get => _question; set { _question = value; OnPropertyChanged(); } }
    public string Answer { get => _answer; set { _answer = value; OnPropertyChanged(); } }
    public bool Error { get => _error; set { _error = value; OnPropertyChanged(); } }

    public InputDialog() {
      InitializeComponent();
      TxtAnswer.Focus();
    }

    public void ShowErrorMessage(string text) {
      TxtAnswer.ToolTip = text;
      Error = true;
    }

    private void TxtAnswer_OnKeyUp(object sender, KeyEventArgs e) => Answer = TxtAnswer.Text;

    public static bool Open(string iconName, string title, string question, string answer, Func<string, string> validator, out string output) {
      var inputDialog = new InputDialog {
        Owner = Application.Current.MainWindow,
        IconName = iconName,
        Title = title,
        Question = question,
        Answer = answer
      };

      inputDialog.BtnDialogOk.Click += delegate {
        var errorMessage = validator(inputDialog.Answer);
        if (string.IsNullOrEmpty(errorMessage))
          inputDialog.DialogResult = true;
        else
          inputDialog.ShowErrorMessage(errorMessage);
      };

      inputDialog.TxtAnswer.SelectAll();

      var result = inputDialog.ShowDialog();
      output = inputDialog.Answer;
      return result == true;
    }
  }
}
