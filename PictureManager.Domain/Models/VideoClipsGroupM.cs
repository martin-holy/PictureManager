using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;

namespace PictureManager.Domain.Models {
  /// <summary>
  /// DB fields: ID|Name|MediaItem|Clips(Items)
  /// </summary>
  public sealed class VideoClipsGroupM : TreeItem, ITreeGroup, IRecord {
    public int Id { get; }
    public MediaItemM MediaItem { get; set; }

    public VideoClipsGroupM(int id, string name) : base(Res.IconFolder, name) {
      Id = id;
    }
  }
}
