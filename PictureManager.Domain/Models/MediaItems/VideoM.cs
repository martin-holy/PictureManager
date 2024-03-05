using MH.Utils.Extensions;
using PictureManager.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Models.MediaItems;

public sealed class VideoM(int id, FolderM folder, string fileName) : RealMediaItemM(id, folder, fileName) {
  public bool HasVideoItems => GetVideoItems().Any();
  public List<VideoClipM> VideoClips { get; set; }
  public List<VideoImageM> VideoImages { get; set; }

  public override string FileNameCache(string name) =>
    string.Concat(name, ".jpg");

  public override IEnumerable<KeywordM> GetKeywords() =>
    GetVideoItems().GetKeywords().Concat(base.GetKeywords()).Distinct();

  public override IEnumerable<PersonM> GetPeople() =>
    GetVideoItems().GetPeople().Concat(base.GetPeople()).Distinct();

  public IEnumerable<SegmentM> GetAllSegments() =>
    GetVideoItems().GetSegments();

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
