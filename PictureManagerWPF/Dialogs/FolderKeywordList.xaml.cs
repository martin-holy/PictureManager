using PictureManager.Domain.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace PictureManager.Dialogs {
  public partial class FolderKeywordList : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public ObservableCollection<FolderM> Items { get; } = new();

    public FolderKeywordList() {
      foreach (var folder in App.Core.FoldersM.All.Where(x => x.IsFolderKeyword).OrderBy(x => x.FullPath)) {
        Items.Add(folder);
      }

      InitializeComponent();
    }

    public static void Open() {
      var fkl = new FolderKeywordList { Owner = App.WMain };
      fkl.ShowDialog();
    }

    private void BtnRemove_OnClick(object sender, RoutedEventArgs e) {
      if (LbFolderKeywords.SelectedItems.Count == 0) return;
      if (!MessageDialog.Show("Remove Confirmation", "Are you sure?", true)) return;

      foreach (var item in LbFolderKeywords.SelectedItems.Cast<FolderM>().ToList()) {
        item.IsFolderKeyword = false;
        Items.Remove(item);
      }

      App.Core.FoldersM.DataAdapter.IsModified = true;
      App.Core.FolderKeywordsM.Load(App.Core.FoldersM.All);
    }
  }
}
