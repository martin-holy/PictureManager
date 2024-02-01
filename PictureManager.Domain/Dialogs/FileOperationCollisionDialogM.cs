using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using PictureManager.Domain.HelperClasses;
using PictureManager.Domain.Models;
using PictureManager.Domain.Models.MediaItems;
using System.IO;
using System.Linq;

namespace PictureManager.Domain.Dialogs {
  public sealed class FileOperationCollisionDialogM : Dialog {
    private string _error;
    private string _fileName;
    private FileInfo _srcFileInfo;
    private FileInfo _destFileInfo;
    private RealMediaItemM _srcMediaItem;
    private RealMediaItemM _destMediaItem;

    public string Error { get => _error; set { _error = value; OnPropertyChanged(); } }
    public string FileName { get => _fileName; set { _fileName = value; OnPropertyChanged(); } }
    public FileInfo SrcFileInfo { get => _srcFileInfo; set { _srcFileInfo = value; OnPropertyChanged(); } }
    public FileInfo DestFileInfo { get => _destFileInfo; set { _destFileInfo = value; OnPropertyChanged(); } }
    public RealMediaItemM SrcMediaItem { get => _srcMediaItem; set { _srcMediaItem = value; OnPropertyChanged(); } }
    public RealMediaItemM DestMediaItem { get => _destMediaItem; set { _destMediaItem = value; OnPropertyChanged(); } }

    public FileOperationCollisionDialogM(string srcFilePath, string destFilePath) : base("The destination already has a file with this name", Res.IconImageMultiple) {
      SrcFileInfo = new(srcFilePath);
      DestFileInfo = new(destFilePath);
      SrcMediaItem = GetMediaItem(srcFilePath);
      DestMediaItem = GetMediaItem(destFilePath);
      FileName = SrcFileInfo.Name;

      Buttons = new DialogButton[] {
        new("Rename", null, new RelayCommand(Rename)),
        new("Replace", null, new RelayCommand(() => Result = (int)CollisionResult.Replace)),
        new("Skip", null, new RelayCommand(() => Result = (int)CollisionResult.Skip)) }; 
    }

    public static CollisionResult Open(string srcFilePath, string destFilePath, ref string fileName) {
      var result = CollisionResult.Skip;
      var outFileName = fileName;

      Tasks.RunOnUiThread(() => {
        var cd = new FileOperationCollisionDialogM(srcFilePath, destFilePath);
        result = (CollisionResult)Show(cd);
        outFileName = cd.FileName;
      }).GetAwaiter().GetResult();

      fileName = outFileName;

      return result == 0 ? CollisionResult.Skip : result;
    }

    private RealMediaItemM GetMediaItem(string filePath) {
      var mi = GetMediaItemByPath(filePath);
      mi.SetInfoBox();
      mi.SetThumbSize();
      return mi;
    }

    private RealMediaItemM GetMediaItemByPath(string path) {
      var lioSep = path.LastIndexOf(Path.DirectorySeparatorChar);
      var folderPath = path[..lioSep];
      var fileName = path[(lioSep + 1)..];
      var folder = Tree.GetByPath(Core.FoldersM.TreeCategory, folderPath, Path.DirectorySeparatorChar) as FolderM;
      var mi = folder?.MediaItems.GetByFileName(fileName);

      if (mi != null) return mi;

      mi = Core.Db.MediaItems.ItemCreate(folder, fileName);
      var mim = new MediaItemMetadata(mi);
      MediaItemsM.ReadMetadata(mim, false);
      if (mim.Success) mim.FindRefs().Wait();

      return mi;
    }

    private void Rename() {
      Error = string.Empty;

      if (string.IsNullOrEmpty(FileName)) {
        Error = "New file name is empty!";
        return;
      }

      if (Path.GetInvalidFileNameChars().Any(FileName.Contains)) {
        Error = "New file name contains incorrect character(s)!";
        return;
      }

      var newFilePath = Path.Combine(DestFileInfo.DirectoryName, FileName);
      if (File.Exists(newFilePath)) {
        DestFileInfo = new(newFilePath);
        DestMediaItem = GetMediaItem(newFilePath);
        return;
      }

      Result = (int)CollisionResult.Rename;
    }
  }
}
