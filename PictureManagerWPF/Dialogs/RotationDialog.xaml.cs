using PictureManager.Domain;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace PictureManager.Dialogs {
  public partial class RotationDialog : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public RotationDialog() {
      InitializeComponent();
    }

    public static new MediaOrientation Show() {
      var result = MediaOrientation.Normal;
      var rd = new RotationDialog { Owner = Application.Current.MainWindow };

      rd.Btn270.Click += delegate {
        rd.Close();
        result = MediaOrientation.Rotate270;
      };

      rd.Btn180.Click += delegate {
        rd.Close();
        result = MediaOrientation.Rotate180;
      };

      rd.Btn90.Click += delegate {
        rd.Close();
        result = MediaOrientation.Rotate90;
      };

      rd.ShowDialog();

      return result;
    }
  }
}
