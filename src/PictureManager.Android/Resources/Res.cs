using System.Collections.Generic;

namespace PictureManager.Android.Resources;

public static class Res {
  public static readonly Dictionary<object, object> IconToColorDic = new() {
    { "default", Resource.Color.colorWhite },
    { Common.Res.IconFolder, Resource.Color.colorFolder },
    { Common.Res.IconFolderStar, Resource.Color.colorFolder },
    { Common.Res.IconFolderLock, Resource.Color.colorFolder },
    { Common.Res.IconFolderPuzzle, Resource.Color.colorFolder },
    { Common.Res.IconFolderOpen, Resource.Color.colorFolder },
    { Common.Res.IconTag, Resource.Color.colorTag },
    { Common.Res.IconTagLabel, Resource.Color.colorTag },
    { Common.Res.IconPeople, Resource.Color.colorPeople },
    { Common.Res.IconPeopleMultiple, Resource.Color.colorPeople },
    { Common.Res.IconDrive, Resource.Color.colorDrive },
    { Common.Res.IconDriveError, Resource.Color.colorDrive },
    { Common.Res.IconCd, Resource.Color.colorDrive }
  };
}