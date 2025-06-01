using MH.UI.Controls;
using MH.UI.Dialogs;
using MH.Utils.BaseClasses;
using PictureManager.Common.Features.Folder;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.FolderKeyword;

public sealed class FolderKeywordsDialog : Dialog {
  private FolderM? _selectedFolder;

  public FolderM? SelectedFolder { get => _selectedFolder; set { _selectedFolder = value; OnPropertyChanged(); } }
  public ObservableCollection<FolderM> Items { get; } = [];
  public RelayCommand<FolderM> SelectCommand { get; }

  public static AsyncRelayCommand OpenCommand { get; } = new(
    _ => ShowAsync(new FolderKeywordsDialog()), null, "Folder Keywords list");

  public FolderKeywordsDialog() : base("Folder Keywords", Res.IconFolderPuzzle) {
    SelectCommand = new(x => SelectedFolder = x);
    Buttons = [
      new(new AsyncRelayCommand(_ => _remove(SelectedFolder!), () => SelectedFolder != null, MH.UI.Res.IconXCross, "Remove")),
      new(CloseCommand, false, true)
    ];

    foreach (var folder in Core.R.FolderKeyword.All.OrderBy(x => x.FullPath))
      Items.Add(folder);
  }

  private async Task _remove(FolderM folder) {
    if (await ShowAsync(new MessageDialog("Remove Confirmation", "Are you sure?", MH.UI.Res.IconQuestion, true)) != 1) return;

    Core.R.FolderKeyword.ItemDelete(folder);
    Items.Remove(folder);
  }
}