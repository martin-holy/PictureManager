using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;
using PictureManager.Properties;

namespace PictureManager.Dialogs {
  /// <summary>
  /// Interaction logic for DirectorySelectDialog.xaml
  /// </summary>
  public partial class DirectorySelectDialog: INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = "") {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private string _iconName = "appbar_folder";
    private string _answer;
    private bool _error;
    private List<string> _paths;

    public string IconName { get { return _iconName;} set { _iconName = value; OnPropertyChanged(); } }
    public string Answer { get { return _answer; } set { _answer = value; OnPropertyChanged(); } }
    public List<string> Paths { get { return _paths; } set { _paths = value; OnPropertyChanged(); } }
    public bool Error { get { return _error; } set { _error = value; OnPropertyChanged(); } }

    public DirectorySelectDialog() {
      InitializeComponent();
      LoadFolders();
      BtnDialogOk.Focus();
    }

    public void ShowErrorMessage(string text) {
      CmbDirectory.ToolTip = text;
      Error = true;
    }

    private void BtnBrowseDir_OnClick(object sender, RoutedEventArgs e) {
      FolderBrowserDialog dir = new FolderBrowserDialog();
      if (dir.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
        var paths = Settings.Default.DirectorySelectFolders.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
        if (!paths.Contains(dir.SelectedPath)) {
          paths.Insert(0, dir.SelectedPath);
        }
        Settings.Default.DirectorySelectFolders = string.Join(Environment.NewLine, paths);
        Settings.Default.Save();
        LoadFolders();
        Answer = dir.SelectedPath;
      }
    }

    private void LoadFolders() {
      Paths = Settings.Default.DirectorySelectFolders.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
      if (Paths.Count != 0)
        CmbDirectory.Text = Paths[0];
    }

    private void BtnDialogOk_OnClick(object sender, RoutedEventArgs e) {
      if (!Directory.Exists(Answer)) {
        ShowErrorMessage("Directory not exists!");
        return;
      }

      DialogResult = true;
    }

    private void DirectorySelectDialog_OnClosing(object sender, CancelEventArgs e) {
      var paths = Settings.Default.DirectorySelectFolders.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
      paths.Remove(CmbDirectory.Text);
      paths.Insert(0, CmbDirectory.Text);
      Settings.Default.DirectorySelectFolders = string.Join(Environment.NewLine, paths);
      Settings.Default.Save();
    }
  }
}
