using System.Windows;
using PictureManager.ViewModels;

namespace PictureManager.Views {
  public partial class VideoClipsV {
    public static readonly DependencyProperty ViewModelProperty =
      DependencyProperty.Register(nameof(ViewModel), typeof(VideoClipsVM), typeof(VideoClipsV));

    public VideoClipsVM ViewModel {
      get => (VideoClipsVM)GetValue(ViewModelProperty);
      set => SetValue(ViewModelProperty, value);
    }

    public VideoClipsV() {
      InitializeComponent();

      Loaded += (_, _) =>
        ViewModel.CtvClips = CtvClips;
    }
  }
}
