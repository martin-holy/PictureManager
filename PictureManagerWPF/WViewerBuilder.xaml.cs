using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace PictureManager {
  /// <summary>
  /// Interaction logic for WViewerBuilder.xaml
  /// </summary>
  public partial class WViewerBuilder : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = "") {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public ViewModel.Viewer Viewer { get; set; }
    public string ViewerTitle { get { return Viewer.Title; } set { Viewer.Title = value; OnPropertyChanged(); } }

    public WViewerBuilder(ViewModel.Viewer viewer) {
      Viewer = viewer;
      InitializeComponent();
    }

    private void BtnOk_OnClick(object sender, RoutedEventArgs e) {
      DialogResult = true;
    }

    private void WViewerBuilder_OnLoaded(object sender, RoutedEventArgs e) {
      DirListAllowed.Folders.Title = "Allowed Folders";
      DirListDenied.Folders.Title = "Denied Folders";
      DirListAllowed.LoadPathsFromList(Viewer.DirsAllowed?.ToList());
      DirListDenied.LoadPathsFromList(Viewer.DirsDenied?.ToList());
    }
  }
}
