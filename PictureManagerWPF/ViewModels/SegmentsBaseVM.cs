using PictureManager.Commands;
using PictureManager.Domain;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public class SegmentsBaseVM {
    public RelayCommand<Segment> SetSegmentPictureCommand { get; }

    public SegmentsBaseVM(Core core) {
      SetSegmentPictureCommand = new(
        async segment => await segment.SetPictureAsync(core.Segments.SegmentSize),
        segment => segment != null);
    }
  }
}
