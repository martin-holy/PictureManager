using System.Collections.ObjectModel;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using SimpleDB;

namespace PictureManager.Domain.Models {
  /// <summary>
  /// DB fields: ID|Name|MediaItem|Clips(Items)
  /// </summary>
  public sealed class VideoClipsGroupM : ObservableObject, IRecord, ITreeBranch {
    #region ITreeBranch implementation
    public ITreeBranch Parent { get; set; }
    public ObservableCollection<ITreeLeaf> Items { get; set; } = new();
    #endregion

    private string _name;

    public string[] Csv { get; set; }

    // DB Fields
    public int Id { get; }
    public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
    public MediaItemM MediaItem { get; set; }

    public VideoClipsGroupM(int id, string name) {
      Id = id;
      Name = name;
    }
  }
}
