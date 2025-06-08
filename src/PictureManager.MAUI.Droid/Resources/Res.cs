using System.Collections.Generic;

namespace PictureManager.MAUI.Droid.Resources;

public static class Res {
  public static readonly Dictionary<object, object> IconToColorDic = new() {
    { "default", Resource.Color.colorWhite },
    { Common.Res.IconFolder, Resource.Color.colorFolder },
    { Common.Res.IconFolderStar, Resource.Color.colorFolder },
    { Common.Res.IconFolderLock, Resource.Color.colorFolder },
    { Common.Res.IconFolderOpen, Resource.Color.colorFolder },
    { Common.Res.IconTag, Resource.Color.colorTag },
    { Common.Res.IconTagLabel, Resource.Color.colorTag },
    { Common.Res.IconDrive, Resource.Color.colorDrive },
    { Common.Res.IconDriveError, Resource.Color.colorDrive },
    { Common.Res.IconCd, Resource.Color.colorDrive }
  };
}