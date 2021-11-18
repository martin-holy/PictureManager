using System.Collections.Generic;
using MH.Utils.BaseClasses;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class VideoClipsGroupM : ObservableObject, IRecord {
    private string _name;

    public string[] Csv { get; set; }

    // DB Fields
    public int Id { get; }
    public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
    public MediaItemM MediaItem { get; set; }
    public List<VideoClipM> Clips { get; set; } = new();

    public VideoClipsGroupM(int id, string name) {
      Id = id;
      Name = name;
    }
  }
}
