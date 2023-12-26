namespace PictureManager.Domain.Models.MediaItems;

public sealed class VideoM : RealMediaItemM {
  private bool _hasVideoClips;
  private bool _hasVideoImages;

  public bool HasVideoClips { get => _hasVideoClips; set { _hasVideoClips = value; OnPropertyChanged(); } }
  public bool HasVideoImages { get => _hasVideoImages; set { _hasVideoImages = value; OnPropertyChanged(); } }

  public VideoM(int id, FolderM folder, string fileName) : base(id, folder, fileName) { }
}
