using PictureManager.Commands;
using PictureManager.Domain;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public class SegmentsBaseVM {
    private readonly Core _core;

    public RelayCommand SetSegmentPictureCommand { get; }

    public SegmentsBaseVM(Core core) {
      _core = core;
      SetSegmentPictureCommand = new(SetSegmentPicture);
    }

    private async void SetSegmentPicture(object parameter) {
      if (parameter is Segment segment)
        await segment.SetPictureAsync(_core.Segments.SegmentSize);
    }
  }
}
