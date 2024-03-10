using MH.Utils;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;
using System.IO;
using System.Linq;

namespace PictureManager.Domain.Services;

public sealed class FolderS {
  public static readonly FolderM FolderPlaceHolder = new(0, string.Empty, null);

  public static string GetDriveIcon(DriveType type) =>
    type switch {
      DriveType.CDRom => Res.IconCd,
      DriveType.Network => Res.IconDrive,
      DriveType.NoRootDirectory or DriveType.Unknown => Res.IconDriveError,
      _ => Res.IconDrive,
    };

  public FolderM[] GetFolders(ITreeItem item, bool recursive) {
    var roots = (item as FolderKeywordM)?.Folders?.ToArray() ?? new[] { (FolderM)item };

    if (!recursive) return roots;

    foreach (var root in roots)
      root.LoadSubFolders(true);

    return roots.SelectMany(x => x.Flatten()).Where(Core.S.Viewer.CanViewerSee).ToArray();
  }
}