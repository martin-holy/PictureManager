using PictureManager.Domain;
using PictureManager.Domain.Extensions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using PictureManager.Domain.Models;
using PictureManager.Domain.Utils;

namespace PictureManager.UserControls {
  public partial class StatusPanel : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private readonly Dictionary<string, string> _dateFormats = new() { { "d", "d. " }, { "M", "MMMM " }, { "y", "yyyy" } };

    public static readonly DependencyProperty IsPinnedProperty = DependencyProperty.Register(nameof(IsPinned), typeof(bool), typeof(StatusPanel), new UIPropertyMetadata(true));

    public bool IsPinned {
      get => (bool)GetValue(IsPinnedProperty);
      set => SetValue(IsPinnedProperty, value);
    }

    public ObservableCollection<string> FilePath {
      get {
        var paths = new ObservableCollection<string>();
        var mi = App.Core.MediaItems.Current;
        if (mi == null) return paths;

        if (App.Ui.AppInfo.AppMode == AppMode.Browser || mi.Folder.FolderKeyword == null) {
          paths.Add(mi.FilePath);
          return paths;
        }

        var fks = new List<FolderKeywordM>();
        Tree.GetThisAndParentRecursive(mi.Folder.FolderKeyword, ref fks);
        fks.Reverse();
        foreach (var fk in fks)
          if (fk.Parent != null) {
            var startIndex = fk.Name.FirstIndexOfLetter();

            if (fk.Name.Length - 1 == startIndex) continue;

            paths.Add(startIndex == 0 ? fk.Name : fk.Name[startIndex..]);
          }

        var fileName = string.IsNullOrEmpty(DateAndTime) ? mi.FileName : mi.FileName[15..];
        paths.Add(fileName);

        return paths;
      }
    }

    public string ZoomActualFormatted => App.WMain?.MediaViewer.FullImage.ZoomActualFormatted;
    public string DateAndTime => Extension.DateTimeFromString(App.Core.MediaItems.Current?.FileName, _dateFormats, "H:mm:ss");
    public ObservableCollection<IconName> Rating { get; } = new();

    public StatusPanel() {
      InitializeComponent();

      App.Core.MediaItems.PropertyChanged += (o, e) => {
        if (e.PropertyName.Equals(nameof(App.Core.MediaItems.Current))) {
          OnPropertyChanged(nameof(DateAndTime));
          OnPropertyChanged(nameof(FilePath));
          UpdateRating();
        }
      };
    }

    public void UpdateRating() {
      Rating.Clear();
      for (var i = 0; i < App.Core.MediaItems.Current?.Rating; i++)
        Rating.Add(IconName.Star);
    }

    private void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) => IsPinned = !IsPinned;
  }
}
