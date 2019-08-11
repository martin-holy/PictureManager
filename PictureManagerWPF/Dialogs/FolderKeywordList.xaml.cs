using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using PictureManager.Database;

namespace PictureManager.Dialogs {
  /// <summary>
  /// Interaction logic for FolderKeywordList.xaml
  /// </summary>
  public partial class FolderKeywordList : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public AppCore ACore => (AppCore) Application.Current.Properties[nameof(AppProperty.AppCore)];
    public ObservableCollection<Folder> Items { get; set; } = new ObservableCollection<Folder>();

    public FolderKeywordList() {
      foreach (var folder in ACore.Folders.All.Where(x => x.IsFolderKeyword)) {
        Items.Add(folder);
      }

      InitializeComponent();
    }

    private void BtnRemove_OnClick(object sender, RoutedEventArgs e) {
      if (LbFolderKeywords.SelectedItems.Count == 0) return;

      var result = MessageBox.Show("Are you sure?", "Remove Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
      if (result != MessageBoxResult.Yes) return;

      foreach (var item in LbFolderKeywords.SelectedItems.Cast<Folder>().ToList()) {
        item.IsFolderKeyword = false;
        Items.Remove(item);
      }

      ACore.Folders.Helper.Table.SaveToFile();
      ACore.FolderKeywords.Load();
    }
  }
}
