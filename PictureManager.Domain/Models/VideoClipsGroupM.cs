using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using SimpleDB;

namespace PictureManager.Domain.Models {
  /// <summary>
  /// DB fields: ID|Name|MediaItem|Clips(Items)
  /// </summary>
  public sealed class VideoClipsGroupM : TreeItem, IRecord, ITreeGroup {
    public string[] Csv { get; set; }

    public int Id { get; }
    public MediaItemM MediaItem { get; set; }

    public VideoClipsGroupM(int id, string name) : base(Res.IconFolder, name) {
      Id = id;
    }
  }
}
