using System.Collections.Generic;

namespace PictureManager.ResourceDictionaries {
  public static class Dictionaries {
    public static Dictionary<object, object> IconNameToBrush = new() {
      { "IconFolder", "ColorBrushFolder" },
      { "IconFolderStar", "ColorBrushFolder" },
      { "IconFolderLock", "ColorBrushFolder" },
      { "IconFolderPuzzle", "ColorBrushFolder" },
      { "IconFolderOpen", "ColorBrushFolder" },
      { "IconTag", "ColorBrushTag" },
      { "IconTagLabel", "ColorBrushTag" },
      { "IconPeople", "ColorBrushPeople" },
      { "IconPeopleMultiple", "ColorBrushPeople" },
      { "IconDrive", "ColorBrushDrive" },
      { "IconDriveError", "ColorBrushDrive" },
      { "IconCd", "ColorBrushDrive" }
    };
  }
}
