using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System;

namespace PictureManager.Domain.Models {
  /// <summary>
  /// DB fields: ID|Name|MediaItem|Clips(Items)
  /// </summary>
  public sealed class VideoClipsGroupM : TreeItem, IEquatable<VideoClipsGroupM>, ITreeGroup {
    #region IEquatable implementation
    public bool Equals(VideoClipsGroupM other) => Id == other?.Id;
    public override bool Equals(object obj) => Equals(obj as VideoClipsGroupM);
    public override int GetHashCode() => Id;
    public static bool operator ==(VideoClipsGroupM a, VideoClipsGroupM b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(VideoClipsGroupM a, VideoClipsGroupM b) => !(a == b);
    #endregion

    public int Id { get; }
    public MediaItemM MediaItem { get; set; }

    public VideoClipsGroupM(int id, string name) : base(Res.IconFolder, name) {
      Id = id;
    }
  }
}
