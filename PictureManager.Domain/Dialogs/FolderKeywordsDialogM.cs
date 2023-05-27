using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using PictureManager.Domain.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace PictureManager.Domain.Dialogs {
  public sealed class FolderKeywordsDialogM : Dialog {
    private readonly Core _core;
    private FolderM _selectedFolder;

    public FolderM SelectedFolder { get => _selectedFolder; set { _selectedFolder = value; OnPropertyChanged(); } }
    public ObservableCollection<FolderM> Items { get; } = new();
    public RelayCommand<FolderM> SelectCommand { get; }

    public static RelayCommand<object> OpenCommand { get; } = new(
      () => Core.DialogHostShow(new FolderKeywordsDialogM(Core.Instance)));

    public FolderKeywordsDialogM(Core core) : base("Folder Keywords", Res.IconFolderPuzzle) {
      _core = core;

      SelectCommand = new(x => SelectedFolder = x);
      var removeCommand = new RelayCommand<object>(
        () => Remove(SelectedFolder),
        () => SelectedFolder != null);

      Buttons = new DialogButton[] {
        new("Remove", Res.IconXCross, removeCommand),
        new("Close", Res.IconXCross, CloseCommand, false, true) };

      foreach (var folder in _core.FolderKeywordsM.DataAdapter.All.OrderBy(x => x.FullPath))
        Items.Add(folder);
    }

    private void Remove(FolderM folder) {
      if (folder == null) return;
      if (Core.DialogHostShow(new MessageDialog("Remove Confirmation", "Are you sure?", Res.IconQuestion, true)) != 1) return;

      _core.FolderKeywordsM.Remove(folder);
      Items.Remove(folder);
    }
  }
}
