using MH.UI.BaseClasses;
using MH.Utils;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;
using System.Linq;

namespace PictureManager.Domain.TreeCategories;

public sealed class VideoClipsTreeCategory : TreeCategory<VideoClipM, VideoClipsGroupM> {
  public VideoClipsTreeCategory() : base(Res.IconMovieClapper, "Clips", (int)Category.VideoClips) {
    DataAdapter = Core.Db.VideoClips;
    GroupDataAdapter = Core.Db.VideoClipsGroups;
    IsExpanded = true;
    CanMoveItem = true;

    DataAdapter.ItemRenamedEvent += (_, _) => UpdateClipsTitles();
    DataAdapter.ItemDeletedEvent += (_, _) => UpdateClipsTitles();
  }

  public override void OnItemSelected(object o) => 
    Core.VideoClipsM.SetCurrentVideoClip(o as VideoClipM);

  public void UpdateClipsTitles() {
    var nr = 0;
    var clips = Items.OfType<VideoClipsGroupM>()
      .SelectMany(g => g.Items.Select(x => x))
      .Concat(Items.OfType<VideoClipM>())
      .Cast<VideoClipM>();

    foreach (var clip in clips) {
      nr++;
      clip.Title = string.IsNullOrEmpty(clip.Name)
        ? $"Clip #{nr}"
        : clip.Name;
    }
  }

  public void ReloadClips(MediaItemM mi) {
    Items = mi != null && Core.Db.VideoClips.MediaItemVideoClips.TryGetValue(mi, out var clips)
      ? clips
      : new();

    UpdateClipsTitles();
    this.SetExpanded<ITreeItem>(true);
    TreeView.RootHolder.Clear();
    TreeView.RootHolder.Add(this);
  }
}