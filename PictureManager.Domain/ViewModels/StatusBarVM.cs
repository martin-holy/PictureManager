using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Domain.Models.MediaItems;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Domain.ViewModels;

public class StatusBarVM(Core core) : ObservableObject {
  private readonly Dictionary<string, string> _dateFormats = new() { { "d", "d. " }, { "M", "MMMM " }, { "y", "yyyy" } };
  private string _fileSize;

  public Core Core { get; } = core;
  public bool IsCountVisible => Core.VM.MediaItemsViews.Current != null || Core.VM.MediaViewer.IsVisible;
  public string FileSize { get => _fileSize; set { _fileSize = value; OnPropertyChanged(); } }

  public ObservableCollection<string> FilePath {
    get {
      var paths = new ObservableCollection<string>();
      if (Core.VM.MediaItem.Current == null) return paths;

      if (Core.VM.MediaItem.Current.Folder.FolderKeyword == null) {
        paths.Add(Core.VM.MediaItem.Current.FilePath);
        return paths;
      }

      var fks = Core.VM.MediaItem.Current.Folder.FolderKeyword.GetThisAndParents().ToList();
      fks.Reverse();
      foreach (var fk in fks)
        if (fk.Parent != null) {
          var startIndex = fk.Name.FirstIndexOfLetter();

          if (fk.Name.Length - 1 == startIndex) continue;

          paths.Add(startIndex == 0 ? fk.Name : fk.Name[startIndex..]);
        }

      var fileName = string.IsNullOrEmpty(DateAndTime)
        ? Core.VM.MediaItem.Current.FileName
        : Core.VM.MediaItem.Current.FileName[15..];
      paths.Add(fileName);

      return paths;
    }
  }

  public string DateAndTime =>
    DateTimeExtensions.DateTimeFromString(Core.VM.MediaItem.Current?.FileName, _dateFormats, "H:mm:ss");

  public ObservableCollection<int> Rating { get; } = [];

  public void UpdateRating() {
    Rating.Clear();
    for (var i = 0; i < Core.VM.MediaItem.Current?.Rating; i++)
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