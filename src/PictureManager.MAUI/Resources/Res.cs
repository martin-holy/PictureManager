using System.Collections.Generic;

namespace PictureManager.MAUI.Resources;

public static class Res {
  public static readonly Dictionary<object, object> IconToBrushDic = new() {
    { "default", "ColorBrushWhite" },
    { Common.Res.IconFolder, "ColorBrushFolder" },
    { Common.Res.IconFolderStar, "ColorBrushFolder" },
    { Common.Res.IconFolderLock, "ColorBrushFolder" },
    { Common.Res.IconFolderOpen, "ColorBrushFolder" },
    { Common.Res.IconTag, "ColorBrushTag" },
    { Common.Res.IconTagLabel, "ColorBrushTag" },
    { Common.Res.IconDrive, "ColorBrushDrive" },
    { Common.Res.IconDriveError, "ColorBrushDrive" },
    { Common.Res.IconCd, "ColorBrushDrive" }
  };
}