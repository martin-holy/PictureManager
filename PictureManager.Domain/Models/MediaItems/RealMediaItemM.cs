using MH.Utils.Extensions;
using System.IO;

namespace PictureManager.Domain.Models.MediaItems;

/// <summary>
/// DB fields: ID|Folder|Name|Width|Height|Orientation|Rating|Comment|GeoName|People|Keywords|IsOnlyInDb
/// </summary>
public class RealMediaItemM : MediaItemM {
  private int _thumbWidth;
  private int _thumbHeight;

  public sealed override FolderM Folder { get; set; }
  public sealed override string FileName { get; set; }
  public override string FilePath => IOExtensions.PathCombine(Folder.FullPath, FileName);
  public override string FilePathCache => GetFilePathCache();
  public override int Width { get; set; }
  public override int Height { get; set; }
  public override int ThumbWidth { get => _thumbWidth; set { _thumbWidth = value; OnPropertyChanged(); } }
  public override int ThumbHeight { get => _thumbHeight; set { _thumbHeight = value; OnPropertyChanged(); } }
  public override int Orientation { get; set; }
  
  public RealMediaItemM(int id, FolderM folder, string fileName) : base(id) {
    Folder = folder;
    FileName = fileName;
  }

  private string GetFilePathCache() =>
    FilePath.Replace(Path.VolumeSeparatorChar.ToString(), Core.Settings.CachePath) +
    (this is ImageM ? string.Empty : ".jpg");
}