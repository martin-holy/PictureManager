using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PictureManager.Common.Features.MediaItem;

namespace PictureManager.Common.Layout;

public class StatusBarVM(CoreVM coreVM) : ObservableObject {
  private readonly Dictionary<string, string> _dateFormats = new() { { "d", "d. " }, { "M", "MMMM " }, { "y", "yyyy" } };
  private string _fileSize = string.Empty;

  public CoreVM CoreVM { get; } = coreVM;
  public MediaItemM? Current { get; private set; }
  public bool IsCountVisible => Core.VM.MediaItem.Views.Current != null || Core.VM.MainWindow.IsInViewMode;
  public string FileSize { get => _fileSize; set { _fileSize = value; OnPropertyChanged(); } }

  public ObservableCollection<string> FilePath {
    get {
      var paths = new ObservableCollection<string>();
      if (Current == null) return paths;

      if (Current.Folder.FolderKeyword == null) {
        paths.Add(Current.FilePath);
        return paths;
      }

      var fks = Current.Folder.FolderKeyword.GetThisAndParents().ToList();
      fks.Reverse();
      foreach (var fk in fks)
        if (fk.Parent != null) {
          var startIndex = fk.Name.FirstIndexOfLetter();

          if (fk.Name.Length - 1 == startIndex) continue;

          paths.Add(startIndex == 0 ? fk.Name : fk.Name[startIndex..]);
        }

      var fileName = string.IsNullOrEmpty(DateAndTime)
        ? Current.FileName
        : Current.FileName[15..];
      paths.Add(fileName);

      return paths;
    }
  }

  public string DateAndTime =>
    DateTimeExtensions.DateTimeFromString(Current?.FileName, _dateFormats, "H:mm:ss");

  public ObservableCollection<int> Rating { get; } = [];

  public void UpdateRating() {
    Rating.Clear();
    for (var i = 0; i < Current?.Rating; i++)
      Rating.Add(0);
  }

  public void UpdateFilePath() =>
    OnPropertyChanged(nameof(FilePath));

  public async Task UpdateFileSize() {
    var items = Core.VM.GetActive<MediaItemM>();
    FileSize = await Task.Run(() => {
      try {
        return items.Any()
          ? IOExtensions.FileSizeToString(items.Sum(mi => new FileInfo(mi.FilePath).Length))
          : string.Empty;
      }
      catch {
        return string.Empty;
      }
    });
  }

  public void Update(MediaItemM? mi) {
    Current = mi;
    OnPropertyChanged(nameof(Current));
    OnPropertyChanged(nameof(DateAndTime));
    OnPropertyChanged(nameof(IsCountVisible));
    UpdateRating();
    UpdateFilePath();
    _ = UpdateFileSize();
  }
}