using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace PictureManager.Dialogs {
  public partial class RotationDialog : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public RotationDialog() {
      InitializeComponent();
    }

    public static new Rotation Show() {
      var result = Rotation.Rotate0;
      var rd = new RotationDialog { Owner = App.WMain };

      rd.Btn270.Click += delegate {
        rd.Close();
        result = Rotation.Rotate270;
      };

      rd.Btn180.Click += delegate {
        rd.Close();
        result = Rotation.Rotate180;
      };

      rd.Btn90.Click += delegate {
        rd.Close();
        result = Rotation.Rotate90;
      };

      rd.ShowDialog();

      return result;
    }
  }
}
