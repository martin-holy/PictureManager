using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;
using System.IO;
using System.Linq;

namespace PictureManager.Domain.Dialogs {
  public sealed class FileOperationCollisionDialogM : ObservableObject, IDialog {
    private string _title = "The destination already has a file with this name";
    private int _result = (int)CollisionResult.Skip;
    private string _error;
    private string _fileName;
    private FileInfo _srcFileInfo;
    private FileInfo _destFileInfo;
    private MediaItemM _srcMediaItem;
    private MediaItemM _destMediaItem;

    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public int Result { get => _result; set { _result = value; OnPropertyChanged(); } }
    public string Error { get => _error; set { _error = value; OnPropertyChanged(); } }
    public string FileName { get => _fileName; set { _fileName = value; OnPropertyChanged(); } }
    public FileInfo SrcFileInfo { get => _srcFileInfo; set { _srcFileInfo = value; OnPropertyChanged(); } }
    public FileInfo DestFileInfo { get => _destFileInfo; set { _destFileInfo = value; OnPropertyChanged(); } }
    public MediaItemM SrcMediaItem { get => _srcMediaItem; set { _srcMediaItem = value; OnPropertyChanged(); } }
    public MediaItemM DestMediaItem { get => _destMediaItem; set { _destMediaItem = value; OnPropertyChanged(); } }

    public RelayCommand<object> RenameCommand { get; }
    public RelayCommand<object> ReplaceCommand { get; }
    public RelayCommand<object> SkipCommand { get; }

    public FileOperationCollisionDialogM(string srcFilePath, string destFilePath) {
      SrcFileInfo = new(srcFilePath);
      DestFileInfo = new(destFilePath);
      SrcMediaItem = GetMediaItem(srcFilePath);
      DestMediaItem = GetMediaItem(destFilePath);
      FileName = SrcFileInfo.Name;

      RenameCommand = new(Rename);
      ReplaceCommand = new(() => Result = (int)CollisionResult.Replace);
      SkipCommand = new(() => Result = (int)CollisionResult.Skip);
    }

    public static CollisionResult Show(string srcFilePath, string destFilePath, ref string fileName) {
      var result = CollisionResult.Skip;
      var outFileName = fileName;

      Core.RunOnUiThread(() => {
        var cd = new FileOperationCollisionDialogM(srcFilePath, destFilePath);
        result = (CollisionResult)Core.DialogHostShow(cd);
        outFileName = cd.FileName;
      }).GetAwaiter().GetResult();

      fileName = outFileName;

      return result;
    }

    private MediaItemM GetMediaItem(string filePath) {
      var mi = Core.Instance.FoldersM.GetMediaItemByPath(filePath);
      mi.SetInfoBox();
      mi.SetThumbSize();
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
