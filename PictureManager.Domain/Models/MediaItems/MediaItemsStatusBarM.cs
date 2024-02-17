using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Domain.Models.MediaItems;

public sealed class MediaItemsStatusBarM : ObservableObject {
  private readonly Dictionary<string, string> _dateFormats = new()
    { { "d", "d. " }, { "M", "MMMM " }, { "y", "yyyy" } };

  private string _fileSize;

  public bool IsVisible => Core.MediaItemsViews.Current != null || Core.MediaViewerM.IsVisible;
  public string FileSize { get => _fileSize; set { _fileSize = value; OnPropertyChanged(); } }

  public ObservableCollection<string> FilePath {
    get {
      var paths = new ObservableCollection<string>();
      if (Core.VM.MediaItems.Current == null) return paths;

      if (Core.VM.MediaItems.Current.Folder.FolderKeyword == null) {
        paths.Add(Core.VM.MediaItems.Current.FilePath);
        return paths;
      }

      var fks = Core.VM.MediaItems.Current.Folder.FolderKeyword.GetThisAndParents().ToList();
      fks.Reverse();
      foreach (var fk in fks)
        if (fk.Parent != null) {
          var startIndex = fk.Name.FirstIndexOfLetter();

          if (fk.Name.Length - 1 == startIndex) continue;

          paths.Add(startIndex == 0 ? fk.Name : fk.Name[startIndex..]);
        }

      var fileName = string.IsNullOrEmpty(DateAndTime)
        ? Core.VM.MediaItems.Current.FileName
        : Core.VM.MediaItems.Current.FileName[15..];
      paths.Add(fileName);

      return paths;
    }
  }

  public string DateAndTime =>
    DateTimeExtensions.DateTimeFromString(Core.VM.MediaItems.Current?.FileName, _dateFormats, "H:mm:ss");

  public ObservableCollection<int> Rating { get; } = new();

  public void UpdateRating() {
    Rating.Clear();
    for (var i = 0; i < Core.VM.MediaItems.Current?.Rating; i++)
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

  public void Update() {
    OnPropertyChanged(nameof(DateAndTime));
    UpdateRating();
    UpdateFilePath();
    _ = UpdateFileSize();
  }
}