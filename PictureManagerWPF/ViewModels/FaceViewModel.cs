using System.Windows.Media.Imaging;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public class FaceViewModel : ObservableObject {
    private Face _face;
    private BitmapSource _picture;
    public Face Face { get => _face; set { _face = value; OnPropertyChanged(); } }
    public BitmapSource Picture { get => _picture; set { _picture = value; OnPropertyChanged(); } }
  }
}
