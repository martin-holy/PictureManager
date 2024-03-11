using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using PictureManager.Common.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace PictureManager.Common.Dialogs {
  public sealed class FolderKeywordsDialogM : Dialog {
    private FolderM _selectedFolder;

    public FolderM SelectedFolder { get => _selectedFolder; set { _selectedFolder = value; OnPropertyChanged(); } }
    public ObservableCollection<FolderM> Items { get; } = new();
    public RelayCommand<FolderM> SelectCommand { get; }

    public static RelayCommand OpenCommand { get; } = new(
      () => Show(new FolderKeywordsDialogM()), null, "Folder Keywords list");

    public FolderKeywordsDialogM() : base("Folder Keywords", Res.IconFolderPuzzle) {
      SelectCommand = new(x => SelectedFolder = x);
      Buttons = new DialogButton[] {
        new(new(() => Remove(SelectedFolder), () => SelectedFolder != null, Res.IconXCross, "Remove")),
        new(CloseCommand, false, true) };

      foreach (var folder in Core.R.FolderKeyword.All.OrderBy(x => x.FullPath))
        Items.Add(folder);
    }

    private void Remove(FolderM folder) {
      if (folder == null) return;
      if (Show(new MessageDialog("Remove Confirmation", "Are you sure?", Res.IconQuestion, true)) != 1) return;

      Core.R.FolderKeyword.ItemDelete(folder);
      Items.Remove(folder);
    }
  }
}
