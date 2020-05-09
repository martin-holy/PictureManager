using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using PictureManager.Domain.Models;

namespace PictureManager.Dialogs {
  /// <summary>
  /// Interaction logic for FolderKeywordList.xaml
  /// </summary>
  public partial class FolderKeywordList : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public ObservableCollection<Folder> Items { get; } = new ObservableCollection<Folder>();

    public FolderKeywordList() {
      foreach (var folder in App.Core.Model.Folders.All.Where(x => x.IsFolderKeyword)) {
        Items.Add(folder);
      }

      InitializeComponent();
    }

    private void BtnRemove_OnClick(object sender, RoutedEventArgs e) {
      if (LbFolderKeywords.SelectedItems.Count == 0) return;
      if (!MessageDialog.Show("Remove Confirmation", "Are you sure?", true)) return;

      foreach (var item in LbFolderKeywords.SelectedItems.Cast<Folder>().ToList()) {
        item.IsFolderKeyword = false;
        Items.Remove(item);
      }

      App.Core.Model.Folders.Helper.Table.SaveToFile();
      App.Core.Model.FolderKeywords.Load();
    }
  }
}
