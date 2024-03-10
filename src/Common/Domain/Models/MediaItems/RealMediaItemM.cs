using MH.Utils;

namespace PictureManager.Domain.Models.MediaItems;

/// <summary>
/// DB fields: ID|Folder|Name|Width|Height|Orientation|Rating|Comment|GeoName|People|Keywords|IsOnlyInDb
/// </summary>
public class RealMediaItemM(int id, FolderM folder, string fileName) : MediaItemM(id) {
  private int _thumbWidth;
  private int _thumbHeight;
  private int _width;
  private int _height;
  private Orientation _orientation;

  public sealed override FolderM Folder { get; set; } = folder;
  public sealed override string FileName { get; set; } = fileName;
  public override int Width { get => _width; set { _width = value; OnPropertyChanged(); } }
  public override int Height { get => _height; set { _height = value; OnPropertyChanged(); } }
  public override int ThumbWidth { get => _thumbWidth; set { _thumbWidth = value; OnPropertyChanged(); } }
  public override int ThumbHeight { get => _thumbHeight; set { _thumbHeight = value; OnPropertyChanged(); } }
  public override Orientation Orientation { get => _orientation; set { _orientation = value; OnPropertyChanged(); } }
  public bool IsOnlyInDb { get; set; } // used when metadata can't be read/write
}