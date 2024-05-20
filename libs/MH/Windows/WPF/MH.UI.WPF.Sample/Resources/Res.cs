using System.Collections.Generic;

namespace MH.UI.WPF.Sample.Resources;

public static class Res {
  public static readonly Dictionary<object, object> IconToBrushDic = new() {
    { "default", "ColorBrushWhite" },
    { Icons.Folder, "ColorBrushFolder" },
    { Icons.FolderStar, "ColorBrushFolder" },
    { Icons.FolderLock, "ColorBrushFolder" },
    { Icons.FolderOpen, "ColorBrushFolder" },
    { Icons.Tag, "ColorBrushTag" },
    { Icons.TagLabel, "ColorBrushTag" },
    { Icons.Drive, "ColorBrushDrive" },
    { Icons.DriveError, "ColorBrushDrive" },
    { Icons.Cd, "ColorBrushDrive" }
  };
}