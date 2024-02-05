using MH.Utils.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Models.MediaItems;

public sealed class VideoM : RealMediaItemM {
  public bool HasVideoItems => GetVideoItems().Any();
  public List<VideoClipM> VideoClips { get; set; }
  public List<VideoImageM> VideoImages { get; set; }

  public VideoM(int id, FolderM folder, string fileName) : base(id, folder, fileName) { }

  public override IEnumerable<PersonM> GetPeople() =>
    GetVideoItems()
      .GetPeople()
      .Concat(People.EmptyIfNull().Concat(Segments.GetPeople()))
      .Distinct();

  public override void SetInfoBox(bool update = false) {
    base.SetInfoBox(update);
    OnPropertyChanged(nameof(HasVideoItems));
  }

  public IEnumerable<VideoItemM> GetVideoItems() =>
    VideoClips.EmptyIfNull().Cast<VideoItemM>().Concat(VideoImages.EmptyIfNull());

  public void Toggle(VideoClipM item) {
    VideoClips = VideoClips.Toggle(item, true);
    OnPropertyChanged(nameof(HasVideoItems));
  }

  public void Toggle(VideoImageM item) {
    VideoImages = VideoImages.Toggle(item, true);
    OnPropertyChanged(nameof(HasVideoItems));
  }
}
